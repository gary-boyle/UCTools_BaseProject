using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.Config.ScriptableObjects;
using GameFramework.EventSystem.Interfaces;

namespace GameFramework.Components.Controllers.Camera
{
    /// <summary>
    /// Third-person camera control using Cinemachine 3.1+.
    /// Provides orbiting camera with follow behavior and collision detection.
    /// </summary>
    public class ThirdPersonCameraControl : MonoBehaviour, ICameraControl
    {
        #region Serialized Fields
        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Transform _lookAtTarget;
        
        [Header("Orbit Settings")]
        [SerializeField] private float _mouseSensitivityMultiplier = 0.01f;
        [SerializeField] private float _orbitSpeed = 2.0f;
        [SerializeField] private float _minVerticalAngle = -30f;
        [SerializeField] private float _maxVerticalAngle = 60f;
        [SerializeField] private bool _invertYAxis = false;
        
        [Header("Distance Settings")]
        [SerializeField] private float _followDistance = 5.0f;
        [SerializeField] private float _minDistance = 2.0f;
        [SerializeField] private float _maxDistance = 10.0f;
        [SerializeField] private float _zoomSpeed = 2.0f;
        [SerializeField] private float _zoomSmoothTime = 0.2f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private IPauseService _pauseService;
        private InputSettings_SO _inputSettings;
        private IEventSystem _eventSystem;
        
        // Look state  
        private Vector2 _lookInput = Vector2.zero;
        private float _currentOrbitY = 0f;
        
        // Distance state
        private float _currentDistance = 5.0f;
        private float _targetDistance = 5.0f;
        private float _zoomVelocity = 0f;
        
        // Input processing
        private float _globalMouseSensitivity = 1.0f;
        private bool _globalInvertYAxis = false;
        private bool _inputEnabled = true;
        
        // Cursor state management
        private bool _wasLockedBeforePause = true;
        
        // Component state
        private bool _isInitialized = false;
        #endregion

        #region Public Properties
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        public float MouseSensitivityMultiplier 
        { 
            get => _mouseSensitivityMultiplier; 
            set => _mouseSensitivityMultiplier = Mathf.Clamp(value, 0.1f, 5.0f); 
        }
        public float EffectiveMouseSensitivity => _globalMouseSensitivity * _mouseSensitivityMultiplier;
        
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get services
            _pauseService = GameManager.GetService<IPauseService>();
            _eventSystem = GameManager.GetService<IEventSystem>();
            
            // Get input settings
            _inputSettings = SettingsRegistry.Get<InputSettings_SO>();
            if (_inputSettings != null)
            {
                ApplyInputSettings();
            }
            
            // Set lookAt target to follow target if not specified
            if (_lookAtTarget == null)
            {
                _lookAtTarget = _followTarget;
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            UpdateCamera();
        }
        #endregion

        #region ICameraControl Implementation
        public void Initialize()
        {
            if (_isInitialized) return;
            
            if (_cinemachineCamera == null)
            {
                Debug.LogError("[ThirdPersonCameraControl] CinemachineCamera is required but not assigned.");
                return;
            }
            
            // Initialize distance
            _currentDistance = _followDistance;
            _targetDistance = _followDistance;
            
            _isInitialized = true;
            
            // Subscribe to scroll wheel events for zoom
            if (_eventSystem != null)
            {
                _eventSystem.Subscribe<UIScrollWheelInputEvent>(OnScrollWheel);
            }

            if (_showDebugInfo)
                Debug.Log("[ThirdPersonCameraControl] Initialized successfully");
        }

        public void Cleanup()
        {
            // Unsubscribe from events
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<UIScrollWheelInputEvent>(OnScrollWheel);
            }

            // Clear references
            _pauseService = null;
            _inputSettings = null;
            _eventSystem = null;

            _isInitialized = false;
            
            if (_showDebugInfo)
                Debug.Log("[ThirdPersonCameraControl] Cleaned up");
        }

        public void HandleLookInput(PlayerLookInputEvent inputEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            _lookInput = inputEvent.LookDelta;
        }

        public void UpdateCamera()
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            ProcessLookInput();
            
            // Reset input after processing
            _lookInput = Vector2.zero;
        }

        public void SetTarget(Transform target)
        {
            _followTarget = target;
            _lookAtTarget = target; // Also use as look target
            
            if (_cinemachineCamera != null && target != null)
            {
                _cinemachineCamera.Follow = target;
                _cinemachineCamera.LookAt = target;
            }
            
            if (_showDebugInfo && target != null)
            {
                Debug.Log($"[ThirdPersonCameraControl] Set camera follow target to: {target.name}");
            }
        }

        public Transform GetCameraTransform()
        {
            return _cinemachineCamera?.transform;
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _lookInput = Vector2.zero;
            }
        }
        #endregion

        #region Private Methods
        
        private void ApplyInputSettings()
        {
            if (_inputSettings == null) return;
            
            _globalMouseSensitivity = _inputSettings.GetMouseSensitivity();
            _globalInvertYAxis = _inputSettings.GetInvertYAxis();
            
            if (_showDebugInfo)
            {
                Debug.Log($"[ThirdPersonCameraControl] Applied Input Settings - Global Sensitivity: {_globalMouseSensitivity}, Invert Y: {_globalInvertYAxis}");
            }
        }

        private void ProcessLookInput()
        {
            if (_lookInput.magnitude < 0.01f) return;
            
            // Apply sensitivity - only process vertical input for camera orbit
            float verticalInput = _lookInput.y * EffectiveMouseSensitivity * _orbitSpeed;
            
            // Apply Y-axis inversion for vertical input
            if (_globalInvertYAxis || _invertYAxis)
            {
                verticalInput *= -1f;
            }
            
            // Update vertical orbit only (horizontal rotation is now handled by movement component)
            _currentOrbitY -= verticalInput;
            
            // Clamp vertical orbit
            _currentOrbitY = Mathf.Clamp(_currentOrbitY, _minVerticalAngle, _maxVerticalAngle);
            
            if (_showDebugInfo)
            {
                Debug.Log($"[ThirdPersonCameraControl] Camera vertical orbit: {_currentOrbitY}°");
            }
        }

        private void OnScrollWheel(UIScrollWheelInputEvent scrollEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            float scrollInput = scrollEvent.ScrollDelta.y;
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                _targetDistance -= scrollInput * _zoomSpeed;
                _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
            }
        }

        #endregion

    }
}
