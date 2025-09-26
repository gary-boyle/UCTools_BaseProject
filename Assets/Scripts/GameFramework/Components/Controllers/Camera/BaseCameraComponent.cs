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
        
        [Header("Zoom Settings")]
        [SerializeField] protected float _zoomSpeed = 2.0f;
        [SerializeField] protected float _minZoom = 5.0f;
        [SerializeField] protected float _maxZoom = 20.0f;
        [SerializeField] protected float _zoomSmoothTime = 0.2f;
        [SerializeField] protected float _zoomSmoothness = 8.0f;

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

        #region Zoom
        protected float _currentZoom = 10.0f;
        protected float _targetZoom = 10.0f;
        protected float _zoomVelocity = 0f;
        #endregion

        protected UnityEngine.Camera _mainCamera;
        
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
            
            _mainCamera = GameManager.GetService<IGameDataService>().GetMainCamera();
            
            SubscribeToEvents();
            InitializeCameraSpecific();
            _isInitialized = true;
        }


        public virtual void Cleanup()
        {
            UnsubscribeFromEvents();
            CleanupCameraSpecific();
            _isInitialized = false;
        }

        private void SubscribeToEvents()
        {
            if (_eventSystem != null)
            {
                _eventSystem.Subscribe<ScrollWheelInputEvent>(OnScrollWheel);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<ScrollWheelInputEvent>(OnScrollWheel);
            }
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

            switch (inputEvent.Phase)
            {
                case UnityEngine.InputSystem.InputActionPhase.Performed:
                    _lookInput = inputEvent.LookDelta ;
                    break;
                // case UnityEngine.InputSystem.InputActionPhase.Canceled:
                //     _lookInput = Vector2.zero;
                //     break;
            }
        }

        private void OnScrollWheel(ScrollWheelInputEvent scrollEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            float scrollInput = scrollEvent.ScrollDelta.y;
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                _targetZoom -= scrollInput * _zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }
            Debug.Log("_targetZoom: " + _targetZoom);
            
        }
        public virtual void UpdateCamera()
        {
            if (!_isInitialized || IsPaused) return;
            UpdateZoom();
            ProcessLookInput();
            _lookInput = Vector2.zero;
            UpdateCameraSpecific();
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

        protected abstract void UpdateZoom();
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
