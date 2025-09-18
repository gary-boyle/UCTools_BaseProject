using UnityEngine;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;
using GameFramework.Core;

namespace GameFramework.Components
{
    /// <summary>
    /// MonoBehaviour that can be attached to a cube to make it clickable.
    /// When clicked, changes the cube's color and increments an integer value.
    /// Implements ISaveable to persist state between game sessions.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ClickableCube : MonoBehaviour, ISaveable
    {
        #region ISaveable Implementation
        public string SaveKey => $"ClickableCube_{UniqueID}";
        public string TypeName => typeof(ClickableCube).Name;
        #endregion

        #region Private Fields
        [SerializeField] private string _uniqueID;
        [SerializeField] private Color _cubeColor = Color.white;
        [SerializeField] private int _cubeValue = 0;
        
        private Renderer _renderer;
        private Camera _mainCamera;
        private ISaveDataRegistry _saveDataRegistry;
        private bool _isRegisteredWithSaveSystem = false;
        
        [Header("Click Configuration")]
        [SerializeField] private Color[] _colorCycle = { 
            Color.white, Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan 
        };
        
        [Header("Display Settings")]
        [SerializeField] private bool _showValueOnScreen = true;
        [SerializeField] private Vector3 _textOffset = new Vector3(0, 2f, 0);
        #endregion

        #region Public Properties
        public string UniqueID
        {
            get => _uniqueID;
            private set
            {
                if (string.IsNullOrEmpty(value) || !UniqueIDGenerator.IsValidUniqueID(value))
                {
                    Debug.LogError($"[ClickableCube] Invalid UniqueID assigned: {value}");
                    return;
                }
                _uniqueID = value;
            }
        }
        
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
        private void Awake()
        {
            // Generate unique ID if not already set
            if (string.IsNullOrEmpty(_uniqueID))
            {
                GenerateUniqueId();
            }
            
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
        
        private async void Start()
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
            
            // Register with save system
            await RegisterWithSaveSystemAsync();
        }
        
        private void Update()
        {
            // Check for mouse click
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from save system
            UnregisterFromSaveSystem();
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

        #region Save System Integration
        private async System.Threading.Tasks.Task RegisterWithSaveSystemAsync()
        {
            try
            {
                // Get the SaveDataRegistry service
                _saveDataRegistry = await GameManager.GetServiceAsync<ISaveDataRegistry>();
                
                if (_saveDataRegistry != null && !_isRegisteredWithSaveSystem)
                {
                    bool registered = _saveDataRegistry.RegisterSaveable(this);
                    _isRegisteredWithSaveSystem = registered;
                    
                    if (registered)
                    {
                        Debug.Log($"[ClickableCube] {gameObject.name} registered with save system (Key: {SaveKey})");
                    }
                    else
                    {
                        Debug.LogWarning($"[ClickableCube] Failed to register {gameObject.name} with save system");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ClickableCube] Error registering with save system: {ex.Message}");
            }
        }
        
        private void UnregisterFromSaveSystem()
        {
            if (_saveDataRegistry != null && _isRegisteredWithSaveSystem)
            {
                _saveDataRegistry.DeregisterSaveable(this);
                _isRegisteredWithSaveSystem = false;
                Debug.Log($"[ClickableCube] {gameObject.name} unregistered from save system");
            }
        }
        #endregion

        #region ISaveable Methods
        public object GetSaveData()
        {
            return new ClickableCubeSaveData(_uniqueID, _cubeColor, _cubeValue);
        }

        public void LoadSaveData(object data)
        {
            if (data == null)
            {
                Debug.LogWarning($"[ClickableCube] Cannot load null save data for {gameObject.name}");
                return;
            }

            try
            {
                ClickableCubeSaveData saveData;
                
                if (data is ClickableCubeSaveData directData)
                {
                    saveData = directData;
                }
                else
                {
                    // Try JSON conversion as fallback
                    var json = JsonUtility.ToJson(data);
                    saveData = JsonUtility.FromJson<ClickableCubeSaveData>(json);
                }

                // Restore state
                _uniqueID = saveData.uniqueID;
                _cubeValue = saveData.cubeValue;
                CubeColor = saveData.cubeColor; // This will also update the visual

                Debug.Log($"[ClickableCube] {gameObject.name} loaded save data - Value: {_cubeValue}, Color: {_cubeColor}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ClickableCube] Failed to load save data for {gameObject.name}: {ex.Message}");
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Generates a new unique ID for this cube
        /// </summary>
        private void GenerateUniqueId()
        {
            UniqueID = UniqueIDGenerator.GenerateUniqueID("cube");
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
