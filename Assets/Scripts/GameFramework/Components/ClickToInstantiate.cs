using GameFramework.Core;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using UnityEngine;

namespace GameFramework.Components
{
    /// <summary>
    /// MonoBehaviour that instantiates a prefab at the location where the user clicks.
    /// Supports both raycast-based placement and fixed depth placement.
    /// </summary>
    public class ClickToInstantiate : MonoBehaviour
    {
        [Header("Prefab Settings")]
        [SerializeField] private GameObject prefabToInstantiate;
        
        [Header("Placement Settings")]
        [SerializeField] private PlacementMode placementMode = PlacementMode.Raycast;
        [SerializeField] private LayerMask raycastLayerMask = -1;
        [SerializeField] private float fixedDepth = 0f;
        
        [Header("Input Settings")]
        [SerializeField] private MouseButton mouseButton = MouseButton.Left;
        
        [Header("Optional Settings")]
        [SerializeField] private bool alignToSurfaceNormal = true;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        [SerializeField] private Transform parentTransform;
        
        private Camera mainCamera;
        private IInputManager _inputManager;

        public enum PlacementMode
        {
            Raycast,        // Places on surfaces hit by raycast
            FixedDepth      // Places at fixed Z depth from camera
        }
        
        public enum MouseButton
        {
            Left = 0,
            Right = 1,
            Middle = 2
        }
        
        private void Start()
        {
            _inputManager = GameManager.GetService<IInputManager>();
            
            // Get main camera reference
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
                if (mainCamera == null)
                {
                    Debug.LogError("[ClickToInstantiate] No camera found in scene!");
                    enabled = false;
                }
            }
            
            // Validate prefab
            if (prefabToInstantiate == null)
            {
                Debug.LogWarning("[ClickToInstantiate] No prefab assigned to instantiate!");
            }
        }
        
        private void Update()
        {
            if (_inputManager.GetCurrentContext() != InputContext.Player) return;
            
            // Check for mouse click
            if (UnityEngine.Input.GetMouseButtonDown((int)mouseButton))
            {
                HandleClick();
            }
        }
        
        private void HandleClick()
        {
            if (prefabToInstantiate == null || mainCamera == null)
                return;
            
            Vector3 mousePosition = UnityEngine.Input.mousePosition;
            Vector3 worldPosition;
            Quaternion rotation = Quaternion.identity;
            
            switch (placementMode)
            {
                case PlacementMode.Raycast:
                    if (GetWorldPositionFromRaycast(mousePosition, out worldPosition, out rotation))
                    {
                        InstantiatePrefab(worldPosition, rotation);
                    }
                    break;
                    
                case PlacementMode.FixedDepth:
                    worldPosition = GetWorldPositionFromFixedDepth(mousePosition);
                    InstantiatePrefab(worldPosition, rotation);
                    break;
            }
        }
        
        private bool GetWorldPositionFromRaycast(Vector3 screenPosition, out Vector3 worldPosition, out Quaternion rotation)
        {
            worldPosition = Vector3.zero;
            rotation = Quaternion.identity;
            
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, raycastLayerMask))
            {
                worldPosition = hit.point;
                
                if (alignToSurfaceNormal)
                {
                    // Align to surface normal
                    rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(rotationOffset);
                }
                else
                {
                    rotation = Quaternion.Euler(rotationOffset);
                }
                
                return true;
            }
            
            return false;
        }
        
        private Vector3 GetWorldPositionFromFixedDepth(Vector3 screenPosition)
        {
            // Convert screen position to world position at fixed depth
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
                screenPosition.x, 
                screenPosition.y, 
                fixedDepth
            ));
            
            return worldPosition;
        }
        
        private void InstantiatePrefab(Vector3 position, Quaternion rotation)
        {
            GameObject instantiatedObject = Instantiate(prefabToInstantiate, position, rotation);
            
            // Set parent if specified
            if (parentTransform != null)
            {
                instantiatedObject.transform.SetParent(parentTransform);
            }
            
            // Optional: Add any additional setup here
            OnPrefabInstantiated(instantiatedObject);
        }
        
        /// <summary>
        /// Called after a prefab is instantiated. Override this for additional setup.
        /// </summary>
        /// <param name="instantiatedObject">The newly instantiated GameObject</param>
        protected virtual void OnPrefabInstantiated(GameObject instantiatedObject)
        {
            // Override this method for custom behavior after instantiation
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw a sphere to show the click area when selected
            if (mainCamera != null && placementMode == PlacementMode.FixedDepth)
            {
                Gizmos.color = Color.yellow;
                Vector3 centerPoint = mainCamera.transform.position + mainCamera.transform.forward * fixedDepth;
                Gizmos.DrawWireSphere(centerPoint, 0.5f);
            }
        }
        #endif
    }
}
