using UnityEngine;
using GameFramework.SaveSystem;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;
using GameFramework.SaveSystem.Attributes;
using GameFramework.Core;

namespace GameFramework.Components
{
    /// <summary>
    /// MonoBehaviour that can be attached to a cube to make it clickable.
    /// When clicked, changes the cube's color and increments an integer value.
    /// Uses the new clean save system with direct field storage instead of nested JSON.
    /// </summary>
    [SaveableType(typeof(ClickableCubeRuntimeSaveData))]
    [RequireComponent(typeof(Collider))]
    public class ClickableCube : SaveableBase
    {
        #region Private Fields
        [SerializeField] private Color _cubeColor = Color.white;
        [SerializeField] private int _cubeValue = 0;
        
        private Renderer _renderer;
        private Camera _mainCamera;
        
        [Header("Click Configuration")]
        [SerializeField] private Color[] _colorCycle = { 
            Color.white, Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan 
        };
        
        [Header("Display Settings")]
        [SerializeField] private bool _showValueOnScreen = true;
        [SerializeField] private Vector3 _textOffset = new Vector3(0, 2f, 0);
        #endregion

        #region Public Properties
        
        public Color CubeColor
        {
            get => _cubeColor;
            private set
            {
                _cubeColor = value;
                UpdateVisualColor();
            }
        }
        
        public int CubeValue
        {
            get => _cubeValue;
            private set => _cubeValue = value;
        }
        #endregion

        #region Unity Lifecycle
        protected override void OnAwakeCustom()
        {
            // Get required components
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                Debug.LogError($"[ClickableCube] No Renderer component found on {gameObject.name}!");
                enabled = false;
                return;
            }
            
            // Ensure we have a collider for clicking
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogError($"[ClickableCube] No Collider component found on {gameObject.name}!");
                enabled = false;
                return;
            }
        }
        
        protected override void OnStartCustom()
        {
            // Get main camera reference
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindObjectOfType<Camera>();
                if (_mainCamera == null)
                {
                    Debug.LogError("[ClickableCube] No camera found in scene!");
                }
            }
            
            // Initialize visual state
            UpdateVisualColor();
        }
        
        private void Update()
        {
            // Check for mouse click
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }
        }
        
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw a wireframe cube to show the clickable area
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
            
            // Draw text position
            if (_showValueOnScreen)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(transform.position + _textOffset, 0.2f);
            }
        }
        #endif
        #endregion

        #region Click Handling
        private void HandleMouseClick()
        {
            if (_mainCamera == null) return;
            
            // Cast ray from camera to mouse position
            Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if we clicked on this cube
                if (hit.collider.gameObject == gameObject)
                {
                    OnCubeClicked();
                }
            }
        }
        
        private void OnCubeClicked()
        {
            // Increment the value
            _cubeValue++;
            
            // Cycle through colors
            int colorIndex = _cubeValue % _colorCycle.Length;
            CubeColor = _colorCycle[colorIndex];
            
            Debug.Log($"[ClickableCube] {gameObject.name} clicked! Value: {_cubeValue}, Color: {_cubeColor}");
            
            // Optional: Add particle effects, sounds, etc. here
            OnValueChanged();
        }
        
        /// <summary>
        /// Called when the cube's value or color changes. Override for custom behavior.
        /// </summary>
        protected virtual void OnValueChanged()
        {
            // Override this method for custom behavior when values change
        }
        #endregion

        #region Visual Updates
        private void UpdateVisualColor()
        {
            if (_renderer != null && _renderer.material != null)
            {
                _renderer.material.color = _cubeColor;
            }
        }
        #endregion


        #region New Save System Implementation
        protected override RuntimeObjectSaveData CreateSpecificRuntimeSaveData()
        {
            return new ClickableCubeRuntimeSaveData(UniqueID, PrefabGUID)
            {
                cubeColor = _cubeColor,
                cubeValue = _cubeValue
            };
        }
        
        protected override void LoadSpecificRuntimeSaveData(RuntimeObjectSaveData saveData)
        {
            if (saveData is ClickableCubeRuntimeSaveData cubeData)
            {
                _cubeValue = cubeData.cubeValue;
                CubeColor = cubeData.cubeColor; // This will also update the visual
                
                Debug.Log($"[ClickableCube] Loaded specific save data - Value: {_cubeValue}, Color: {_cubeColor}");
            }
            else
            {
                Debug.LogWarning($"[ClickableCube] Expected ClickableCubeRuntimeSaveData but got: {saveData?.GetType().Name}");
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Gets the prefix used for unique ID generation
        /// </summary>
        protected override string GetUniqueIdPrefix()
        {
            return "cube";
        }
        
        /// <summary>
        /// Manually set the cube's value and color (useful for testing or initialization)
        /// </summary>
        public void SetValues(int value, Color color)
        {
            _cubeValue = value;
            CubeColor = color;
        }
        
        /// <summary>
        /// Reset the cube to its initial state
        /// </summary>
        public void ResetCube()
        {
            _cubeValue = 0;
            CubeColor = Color.white;
        }
        #endregion
    }
}
