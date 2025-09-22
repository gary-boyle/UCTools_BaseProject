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
        [SerializeField] private float _mouseSensitivityMultiplier = 1.0f;
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
        
        [Header("Collision")]
        [SerializeField] private bool _enableCollisionDetection = true;
        [SerializeField] private LayerMask _collisionLayers = -1;
        [SerializeField] private float _collisionRadius = 0.3f;
        
        [Header("Cursor Settings")]
        [SerializeField] private bool _lockCursor = true;
        [SerializeField] private bool _hideCursor = true;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private IPauseService _pauseService;
        private InputSettings_SO _inputSettings;
        private IEventSystem _eventSystem;
        
        // Look state
        private Vector2 _lookInput = Vector2.zero;
        private float _currentOrbitX = 0f;
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
        
        // Cinemachine components
        private CinemachineOrbitalFollow _orbitalFollow;
        private CinemachineRotationComposer _composer;
        private CinemachineDeoccluder _collider;
        
        // Component state
        private bool _isInitialized = false;
        #endregion

        #region Public Properties
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        public Vector2 CurrentLookInput => _lookInput;
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
            
            // Setup Cinemachine camera for third-person
            //SetupCinemachineCamera();
            
            // Initialize cursor state
            if (_lockCursor)
            {
                SetCursorLocked(true);
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
            // Ensure cursor is unlocked
            SetCursorLocked(false);
            
            // Unsubscribe from events
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<UIScrollWheelInputEvent>(OnScrollWheel);
            }

            // Clear references
            _pauseService = null;
            _inputSettings = null;
            _eventSystem = null;
            _orbitalFollow = null;
            _composer = null;
            _collider = null;
            
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
            ApplyOrbitRotation();
            UpdateDistance();
            
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
        private void SetupCinemachineCamera()
        {
            if (_cinemachineCamera == null) return;
            
            // Set follow and look at targets
            if (_followTarget != null)
            {
                _cinemachineCamera.Follow = _followTarget;
                _cinemachineCamera.LookAt = _lookAtTarget ?? _followTarget;
            }
            
            // Configure lens
            _cinemachineCamera.Lens.FieldOfView = 50f; // Standard third-person FOV
            
            // Setup orbital follow component
            _orbitalFollow = _cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();
            if (_orbitalFollow == null)
            {
                _orbitalFollow = _cinemachineCamera.gameObject.AddComponent<CinemachineOrbitalFollow>();
            }
            
            // Configure orbital follow
            _orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
            _orbitalFollow.Radius = _followDistance;
            _orbitalFollow.RadialAxis.Range = new Vector2(_minDistance, _maxDistance);
            _orbitalFollow.RadialAxis.Wrap = false;
            _orbitalFollow.HorizontalAxis.Range = new Vector2(-180f, 180f);
            _orbitalFollow.HorizontalAxis.Wrap = true;
            _orbitalFollow.VerticalAxis.Range = new Vector2(_minVerticalAngle, _maxVerticalAngle);
            _orbitalFollow.VerticalAxis.Wrap = false;
            
            // Setup composer for smooth look-at
            _composer = _cinemachineCamera.GetComponent<CinemachineRotationComposer>();
            if (_composer == null)
            {
                _composer = _cinemachineCamera.gameObject.AddComponent<CinemachineRotationComposer>();
            }
            
            _composer.Composition.ScreenPosition = new Vector2(0.5f, 0.5f);
            //_composer.Composition.HorizontalPosition = 0.5f;
            //_composer.Composition.VerticalPosition = 0.5f;
            //_composer.Composition.LookaheadTime = 0.2f;
            //_composer.Composition.LookaheadSmoothing = 10f;
            
            // Setup collision detection
            if (_enableCollisionDetection)
            {
                _collider = _cinemachineCamera.GetComponent<CinemachineDeoccluder>();
                if (_collider == null)
                {
                    _collider = _cinemachineCamera.gameObject.AddComponent<CinemachineDeoccluder>();
                }
                
                _collider.CollideAgainst = _collisionLayers;
                _collider.MinimumDistanceFromTarget = 0.1f;
                _collider.AvoidObstacles = new CinemachineDeoccluder.ObstacleAvoidance()
                {
                    Enabled = true,
                    Damping = 0.5f,
                    };
                _collider.MinimumDistanceFromTarget = _maxDistance;
            }
            
            if (_showDebugInfo)
                Debug.Log("[ThirdPersonCameraControl] Cinemachine camera configured for third-person");
        }

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
            
            // Apply sensitivity
            Vector2 processedInput = _lookInput * EffectiveMouseSensitivity * _orbitSpeed;
            
            // Apply Y-axis inversion
            if (_globalInvertYAxis || _invertYAxis)
            {
                processedInput.y *= -1f;
            }
            
            // Update orbit values
            _currentOrbitX += processedInput.x;
            _currentOrbitY -= processedInput.y;
            
            // Clamp vertical orbit
            _currentOrbitY = Mathf.Clamp(_currentOrbitY, _minVerticalAngle, _maxVerticalAngle);
        }

        private void ApplyOrbitRotation()
        {
            if (_orbitalFollow == null) return;
            
            // Apply orbital rotation
            _orbitalFollow.HorizontalAxis.Value = _currentOrbitX;
            _orbitalFollow.VerticalAxis.Value = _currentOrbitY;
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

        private void UpdateDistance()
        {
            if (_orbitalFollow == null) return;
            
            // Smooth distance transition
            _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _zoomVelocity, _zoomSmoothTime);
            _orbitalFollow.Radius = _currentDistance;
        }

        private void SetCursorLocked(bool locked)
        {
            if (locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                if (_hideCursor)
                    Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void OnGamePaused()
        {
            if (!_isInitialized) return;
            
            // Clear any pending input
            _lookInput = Vector2.zero;
            
            // Store cursor state and unlock for menu navigation
            _wasLockedBeforePause = Cursor.lockState == CursorLockMode.Locked;
            SetCursorLocked(false);
            
            if (_showDebugInfo)
                Debug.Log("[ThirdPersonCameraControl] Game paused - cursor unlocked");
        }

        public void OnGameResumed()
        {
            if (!_isInitialized) return;
            
            // Restore cursor state if it was locked before pause
            if (_wasLockedBeforePause && _lockCursor)
            {
                SetCursorLocked(true);
            }
            
            if (_showDebugInfo)
                Debug.Log("[ThirdPersonCameraControl] Game resumed - cursor state restored");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the vertical orbit angle limits
        /// </summary>
        public void SetVerticalAngleLimits(float minAngle, float maxAngle)
        {
            _minVerticalAngle = minAngle;
            _maxVerticalAngle = maxAngle;
            
            if (_orbitalFollow != null)
            {
                _orbitalFollow.VerticalAxis.Range = new Vector2(_minVerticalAngle, _maxVerticalAngle);
            }
            
            // Re-clamp current orbit
            _currentOrbitY = Mathf.Clamp(_currentOrbitY, _minVerticalAngle, _maxVerticalAngle);
        }
        
        /// <summary>
        /// Set the distance limits for the camera
        /// </summary>
        public void SetDistanceLimits(float minDistance, float maxDistance)
        {
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            
            if (_orbitalFollow != null)
            {
                _orbitalFollow.RadialAxis.Range = new Vector2(_minDistance, _maxDistance);
            }
            
            // Re-clamp current distance
            _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
        }
        
        /// <summary>
        /// Set the follow distance
        /// </summary>
        public void SetFollowDistance(float distance)
        {
            _followDistance = Mathf.Clamp(distance, _minDistance, _maxDistance);
            _targetDistance = _followDistance;
        }
        
        /// <summary>
        /// Set cursor lock and visibility settings
        /// </summary>
        public void SetCursorSettings(bool lockCursor, bool hideCursor)
        {
            _lockCursor = lockCursor;
            _hideCursor = hideCursor;
            
            if (!IsPaused)
            {
                SetCursorLocked(_lockCursor);
            }
        }
        
        /// <summary>
        /// Enable or disable collision detection
        /// </summary>
        public void SetCollisionDetection(bool enabled)
        {
            _enableCollisionDetection = enabled;
            
            if (_collider != null)
            {
                _collider.enabled = enabled;
            }
        }
        
        /// <summary>
        /// Get the current orbit angles in degrees
        /// </summary>
        public Vector2 GetCurrentOrbit()
        {
            return new Vector2(_currentOrbitX, _currentOrbitY);
        }
        
        /// <summary>
        /// Get the current follow distance
        /// </summary>
        public float GetCurrentDistance()
        {
            return _currentDistance;
        }
        #endregion
    }
}
