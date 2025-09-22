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
    /// Isometric camera control using Cinemachine 3.1+.
    /// Provides smooth following and optional manual panning for isometric/top-down games.
    /// </summary>
    public class IsometricCameraControl : MonoBehaviour, ICameraControl
    {
        #region Serialized Fields
        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _followTarget;
        
        [Header("Isometric Settings")]
        [SerializeField] private float _isometricAngle = 30f; // Standard isometric angle
        [SerializeField] private float _cameraRotationY = 45f; // Y rotation for isometric view
        [SerializeField] private bool _lockRotation = true;
        
        [Header("Follow Settings")]
        [SerializeField] private bool _followPlayer = true;
        [SerializeField] private Vector3 _followOffset = new Vector3(0, 15, -10);
        [SerializeField] private float _followSmoothTime = 0.3f;
        [SerializeField] private Vector3 _lookAtOffset = new Vector3(0, 1, 0);
        
        [Header("Manual Pan Settings")]
        [SerializeField] private bool _enableManualPan = false;
        [SerializeField] private float _mouseSensitivityMultiplier = 1.0f;
        [SerializeField] private float _panSpeed = 5.0f;
        [SerializeField] private float _panSmoothTime = 0.2f;
        
        [Header("Zoom Settings")]
        [SerializeField] private bool _enableZoom = true;
        [SerializeField] private float _zoomSpeed = 2.0f;
        [SerializeField] private float _minZoom = 5.0f;
        [SerializeField] private float _maxZoom = 20.0f;
        [SerializeField] private float _zoomSmoothTime = 0.2f;
        [SerializeField] private bool _orthographicProjection = true;
        
        [Header("Boundaries")]
        [SerializeField] private bool _useBoundaries = false;
        [SerializeField] private Bounds _cameraBounds = new Bounds(Vector3.zero, new Vector3(50, 10, 50));
        
        [Header("Composition")]
        [SerializeField] private float _horizontalDamping = 1.0f;
        [SerializeField] private float _verticalDamping = 1.0f;
        [SerializeField] private float _screenX = 0.5f;
        [SerializeField] private float _screenY = 0.5f;
        [SerializeField] private float _deadZoneWidth = 0.1f;
        [SerializeField] private float _deadZoneHeight = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private IPauseService _pauseService;
        private InputSettings_SO _inputSettings;
        private IEventSystem _eventSystem;
        
        private UnityEngine.Camera _main => UnityEngine.Camera.main;
        // Input state
        private Vector2 _lookInput = Vector2.zero;
        private Vector2 _panInput = Vector2.zero;
        
        // Camera state
        private Vector3 _manualOffset = Vector3.zero;
        private Vector3 _targetOffset = Vector3.zero;
        private Vector3 _offsetVelocity = Vector3.zero;
        
        // Zoom state
        private float _currentZoom = 10.0f;
        private float _targetZoom = 10.0f;
        private float _zoomVelocity = 0f;
        
        // Input processing
        private float _globalMouseSensitivity = 1.0f;
        private bool _inputEnabled = true;
        
        // Cinemachine components
        private CinemachinePositionComposer _positionComposer;
        private CinemachineHardLockToTarget _hardLock;
        
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
                Debug.LogError("[IsometricCameraControl] CinemachineCamera is required but not assigned.");
                return;
            }
            
            // Setup Cinemachine camera for isometric view
            //SetupCinemachineCamera();
            
            // Initialize zoom
            _currentZoom = _minZoom + (_maxZoom - _minZoom) * 0.5f;
            _targetZoom = _currentZoom;
            
            _isInitialized = true;
            
            // Subscribe to scroll wheel events for zoom
            if (_eventSystem != null)
            {
                _eventSystem.Subscribe<UIScrollWheelInputEvent>(OnScrollWheel);
            }

            if (_showDebugInfo)
                Debug.Log("[IsometricCameraControl] Initialized successfully");
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
            _positionComposer = null;
            _hardLock = null;
            
            _isInitialized = false;
            
            if (_showDebugInfo)
                Debug.Log("[IsometricCameraControl] Cleaned up");
        }

        public void HandleLookInput(PlayerLookInputEvent inputEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            _lookInput = inputEvent.LookDelta;
        }

        public void UpdateCamera()
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            ProcessInputs();
            HandleManualPan();
            UpdateCameraPosition();
            UpdateZoom();
            
            // Reset input after processing
            _lookInput = Vector2.zero;
        }

        public void SetTarget(Transform target)
        {
            _followTarget = target;
            
            if (_cinemachineCamera != null && target != null)
            {
                _cinemachineCamera.Follow = target;
                _cinemachineCamera.LookAt = target;
                
                // Reset manual offset when switching targets
                _manualOffset = Vector3.zero;
                _targetOffset = Vector3.zero;
            }
        }

        public Transform GetCameraTransform()
        {
            return _cinemachineCamera.transform;
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _lookInput = Vector2.zero;
                _panInput = Vector2.zero;
            }
        }
        #endregion

        #region Private Methods
        private void SetupCinemachineCamera()
        {
            if (_cinemachineCamera == null) return;
            
            // Set follow target
            if (_followTarget != null)
            {
                _cinemachineCamera.Follow = _followTarget;
                _cinemachineCamera.LookAt = _followTarget;
            }
            
            // Configure projection
            if (_orthographicProjection)
            {
                var cam = GameManager.GetService<IGameDataService>().GetMainCamera();
                cam.orthographic = true;
                _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
            }
            else
            {
                var cam = GameManager.GetService<IGameDataService>().GetMainCamera();
                cam.orthographic = false;
                _cinemachineCamera.Lens.FieldOfView = 60f;
            }
            
            // Set isometric camera angle and rotation
            Transform cameraTransform = _cinemachineCamera.transform;
            cameraTransform.rotation = Quaternion.Euler(_isometricAngle, _cameraRotationY, 0f);
            
            if (_followPlayer && _followTarget != null)
            {
                SetupFollowMode();
            }
            else
            {
                SetupFreeMode();
            }
            
            if (_showDebugInfo)
                Debug.Log("[IsometricCameraControl] Cinemachine camera configured for isometric view");
        }

        private void SetupFollowMode()
        {
            // Remove hard lock if it exists
            if (_hardLock != null)
            {
                Object.DestroyImmediate(_hardLock);
                _hardLock = null;
            }
            
            // Setup position composer for smooth following with dead zones
            _positionComposer = _cinemachineCamera.GetComponent<CinemachinePositionComposer>();
            if (_positionComposer == null)
            {
                _positionComposer = _cinemachineCamera.gameObject.AddComponent<CinemachinePositionComposer>();
            }
            
            _positionComposer.Composition.ScreenPosition = new Vector2(_screenX, _screenY);
            _positionComposer.Composition.DeadZone = new ScreenComposerSettings.DeadZoneSettings
            {
                Enabled = true,
                Size = new Vector2(_deadZoneWidth, _deadZoneHeight)
            };
            _positionComposer.Composition.ScreenPosition = new Vector2(_screenX, _screenY);
            //_positionComposer.Composition.HorizontalPosition = _screenX;
            //_positionComposer.Composition.VerticalPosition = _screenY;
            //_positionComposer.Composition.DeadZoneWidth = _deadZoneWidth;
            //_positionComposer.Composition.DeadZoneHeight = _deadZoneHeight;
            //_positionComposer.Composition.HorizontalDamping = _horizontalDamping;
            //_positionComposer.Composition.VerticalDamping = _verticalDamping;
            //_positionComposer.Composition.ScreenPosition = new Vector2(_screenX, _screenY);
            
            // Set follow offset
            Transform cameraTransform = _cinemachineCamera.transform;
            cameraTransform.position = _followTarget.position + _followOffset;
        }

        private void SetupFreeMode()
        {
            // Remove position composer
            if (_positionComposer != null)
            {
                Object.DestroyImmediate(_positionComposer);
                _positionComposer = null;
            }
            
            // Add hard lock for direct control
            _hardLock = _cinemachineCamera.GetComponent<CinemachineHardLockToTarget>();
            if (_hardLock == null)
            {
                _hardLock = _cinemachineCamera.gameObject.AddComponent<CinemachineHardLockToTarget>();
            }
        }

        private void ApplyInputSettings()
        {
            if (_inputSettings == null) return;
            
            _globalMouseSensitivity = _inputSettings.GetMouseSensitivity();
            
            if (_showDebugInfo)
            {
                Debug.Log($"[IsometricCameraControl] Applied Input Settings - Global Sensitivity: {_globalMouseSensitivity}");
            }
        }

        private void ProcessInputs()
        {
            if (!_enableManualPan) return;
            
            // Convert look input to pan input
            _panInput = _lookInput * EffectiveMouseSensitivity * _panSpeed;
        }

        private void OnScrollWheel(UIScrollWheelInputEvent scrollEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused || !_enableZoom) return;
            
            float scrollInput = scrollEvent.ScrollDelta.y;
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                _targetZoom -= scrollInput * _zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }
        }

        private void UpdateZoom()
        {
            if (!_enableZoom) return;
            
            // Apply smooth zoom
            _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, _zoomSmoothTime);
            
            if (_cinemachineCamera.Lens.Orthographic)
            {
                _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
            }
            else
            {
                // For perspective projection, adjust follow offset based on zoom
                float zoomScale = _currentZoom / (_minZoom + (_maxZoom - _minZoom) * 0.5f);
                Vector3 scaledOffset = _followOffset * zoomScale;
                
                Transform cameraTransform = _cinemachineCamera.transform;
                if (_followTarget != null)
                {
                    cameraTransform.position = _followTarget.position + scaledOffset + _manualOffset;
                }
            }
        }

        private void HandleManualPan()
        {
            if (!_enableManualPan || _panInput.magnitude < 0.01f) return;
            
            // Convert screen space pan input to world space
            Transform cameraTransform = _cinemachineCamera.transform;
            Vector3 right = cameraTransform.right;
            Vector3 forward = Vector3.Cross(right, Vector3.up); // Get forward direction on horizontal plane
            
            // Calculate world space movement
            Vector3 worldPan = (right * _panInput.x + forward * _panInput.y) * Time.deltaTime;
            _targetOffset += worldPan;
            
            // Apply boundaries if enabled
            if (_useBoundaries)
            {
                _targetOffset.x = Mathf.Clamp(_targetOffset.x, _cameraBounds.min.x, _cameraBounds.max.x);
                _targetOffset.z = Mathf.Clamp(_targetOffset.z, _cameraBounds.min.z, _cameraBounds.max.z);
            }
        }

        private void UpdateCameraPosition()
        {
            if (!_enableManualPan) return;
            
            // Smooth manual offset
            _manualOffset = Vector3.SmoothDamp(_manualOffset, _targetOffset, ref _offsetVelocity, _panSmoothTime);
            
            // Apply offset to camera
            if (_followPlayer && _followTarget != null && _positionComposer != null)
            {
                // For follow mode, adjust the composer's screen position based on offset
                // This is a bit complex as we need to convert world offset to screen offset
                Vector2 screenOffset = WorldToScreenOffset(_manualOffset);
                _positionComposer.Composition.ScreenPosition = new Vector2(_screenX + screenOffset.x, _screenY + screenOffset.y);
            }
            else if (!_followPlayer)
            {
                // For free mode, directly adjust camera position
                Transform cameraTransform = _cinemachineCamera.transform;
                Vector3 basePosition = _followTarget != null ? _followTarget.position + _followOffset : _followOffset;
                cameraTransform.position = basePosition + _manualOffset;
            }
        }

        private Vector2 WorldToScreenOffset(Vector3 worldOffset)
        {
            // Simple approximation - convert world offset to screen-space offset
            // This could be more sophisticated but works for basic cases
            // UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (_main == null) return Vector2.zero;
            
            Vector3 screenCenter = _main.WorldToScreenPoint(Vector3.zero);
            Vector3 screenWithOffset = _main.WorldToScreenPoint(worldOffset);
            Vector2 screenDelta = (screenWithOffset - screenCenter);
            
            // Normalize to screen coordinates (0-1)
            screenDelta.x /= Screen.width;
            screenDelta.y /= Screen.height;
            
            return screenDelta;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set whether the camera should follow the player
        /// </summary>
        public void SetFollowPlayer(bool follow)
        {
            if (_followPlayer == follow) return;
            
            _followPlayer = follow;
            
            if (_isInitialized)
            {
                if (_followPlayer)
                {
                    SetupFollowMode();
                }
                else
                {
                    SetupFreeMode();
                }
            }
        }
        
        /// <summary>
        /// Set the follow offset
        /// </summary>
        public void SetFollowOffset(Vector3 offset)
        {
            _followOffset = offset;
        }
        
        /// <summary>
        /// Set the isometric camera angle
        /// </summary>
        public void SetIsometricAngle(float angle)
        {
            _isometricAngle = angle;
            
            if (_cinemachineCamera != null)
            {
                Transform cameraTransform = _cinemachineCamera.transform;
                Vector3 rotation = cameraTransform.eulerAngles;
                rotation.x = _isometricAngle;
                cameraTransform.rotation = Quaternion.Euler(rotation);
            }
        }
        
        /// <summary>
        /// Set the camera Y rotation
        /// </summary>
        public void SetCameraRotationY(float rotationY)
        {
            _cameraRotationY = rotationY;
            
            if (_cinemachineCamera != null)
            {
                Transform cameraTransform = _cinemachineCamera.transform;
                Vector3 rotation = cameraTransform.eulerAngles;
                rotation.y = _cameraRotationY;
                cameraTransform.rotation = Quaternion.Euler(rotation);
            }
        }
        
        /// <summary>
        /// Set zoom level
        /// </summary>
        public void SetZoom(float zoom)
        {
            _targetZoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
        }
        
        /// <summary>
        /// Enable or disable manual panning
        /// </summary>
        public void SetManualPan(bool enabled)
        {
            _enableManualPan = enabled;
            if (!enabled)
            {
                _manualOffset = Vector3.zero;
                _targetOffset = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Reset camera to default position and zoom
        /// </summary>
        public void ResetCamera()
        {
            _manualOffset = Vector3.zero;
            _targetOffset = Vector3.zero;
            _currentZoom = _minZoom + (_maxZoom - _minZoom) * 0.5f;
            _targetZoom = _currentZoom;
        }
        
        /// <summary>
        /// Set camera boundaries
        /// </summary>
        public void SetBoundaries(Bounds bounds)
        {
            _cameraBounds = bounds;
        }
        
        /// <summary>
        /// Get current zoom level
        /// </summary>
        public float GetCurrentZoom()
        {
            return _currentZoom;
        }
        #endregion

        #region Debug
        private void OnDrawGizmos()
        {
            if (!_showDebugInfo) return;
            
            // Draw camera boundaries
            if (_useBoundaries)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(_cameraBounds.center, _cameraBounds.size);
            }
            
            // Draw follow offset
            if (_followTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(_followTarget.position, _followTarget.position + _followOffset);
                
                // Draw manual offset
                if (_manualOffset.magnitude > 0.01f)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 offsetPosition = _followTarget.position + _followOffset + _manualOffset;
                    Gizmos.DrawLine(_followTarget.position + _followOffset, offsetPosition);
                    Gizmos.DrawWireSphere(offsetPosition, 0.5f);
                }
            }
        }
        #endregion
    }
}
