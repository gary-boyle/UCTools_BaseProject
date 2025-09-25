using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;

namespace GameFramework.Components.Controllers.Camera
{
    /// <summary>
    /// Base class for all camera components providing common functionality.
    /// Handles service dependencies, input state management, and pause handling.
    /// </summary>
    public abstract class BaseCameraComponent : MonoBehaviour, ICameraControl
    {
        #region Common Serialized Fields
        [Header("Base Camera Settings")]
        [SerializeField] protected bool _showDebugInfo = false;
        #endregion
        
        #region Common Protected Fields
        protected IPauseService _pauseService;
        protected IEventSystem _eventSystem;
        protected bool _inputEnabled = true;
        protected bool _isInitialized = false;
        
        // Input processing state
        protected Vector2 _lookInput = Vector2.zero;
        #endregion

        #region Common Properties
        public virtual bool IsPaused => _pauseService?.IsPaused ?? false;
        #endregion

        #region Common Unity Lifecycle
        protected virtual void Awake()
        {
            _pauseService = GameManager.GetService<IPauseService>();
            _eventSystem = GameManager.GetService<IEventSystem>();
            
            ValidateServices();
        }
        #endregion

        #region Common Interface Implementation
        public virtual void Initialize()
        {
            if (_isInitialized) return;
            
            InitializeCameraSpecific();
            _isInitialized = true;
        }

        public virtual void Cleanup()
        {
            CleanupCameraSpecific();
            _isInitialized = false;
        }

        public virtual void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            
            // Clear input when disabled
            if (!enabled)
            {
                _lookInput = Vector2.zero;
            }
        }

        public virtual void HandleLookInput(PlayerLookInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused || !_inputEnabled) return;
            
            _lookInput = inputEvent.LookDelta;
        }

        public virtual void UpdateCamera()
        {
            if (!_isInitialized || IsPaused) return;
            
            UpdateCameraSpecific();
            ProcessLookInput();
            _lookInput = Vector2.zero; // Clear input after processing
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Initialize camera-specific functionality
        /// </summary>
        protected abstract void InitializeCameraSpecific();
        
        /// <summary>
        /// Cleanup camera-specific functionality
        /// </summary>
        protected abstract void CleanupCameraSpecific();
        
        /// <summary>
        /// Process look input - implemented by specific camera types
        /// </summary>
        protected abstract void ProcessLookInput();
        
        /// <summary>
        /// Update camera logic specific to this camera type
        /// </summary>
        protected abstract void UpdateCameraSpecific();
        #endregion

        #region Common Protected Methods
        /// <summary>
        /// Validates required services are available
        /// </summary>
        private void ValidateServices()
        {
            if (_pauseService == null)
            {
                Debug.LogWarning($"[{GetType().Name}] IPauseService not found. Pause functionality will not work.");
            }
            
            if (_eventSystem == null)
            {
                Debug.LogWarning($"[{GetType().Name}] IEventSystem not found. Some functionality may not work properly.");
            }
        }
        #endregion

        #region Virtual Debug Methods
        protected virtual void OnDrawGizmos()
        {
            if (!_showDebugInfo) return;
            
            // Base camera debug visualization - override in derived classes for specific debug info
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
        }
        #endregion
    }
}
