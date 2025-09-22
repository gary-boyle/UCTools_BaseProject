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
    /// RTS camera control using Cinemachine 3.1+.
    /// Provides panning, zooming, and rotation capabilities for Real-Time Strategy games.
    /// </summary>
    public class RTSCameraControl : MonoBehaviour, ICameraControl
    {
        #region Serialized Fields
        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraRig; // Empty GameObject to act as camera rig
        
        [Header("Pan Settings")]
        [SerializeField] private float _mouseSensitivityMultiplier = 1.0f;
        [SerializeField] private float _panSpeed = 5.0f;
        [SerializeField] private float _panAcceleration = 10.0f;
        [SerializeField] private float _panDeceleration = 10.0f;
        [SerializeField] private bool _invertPanX = false;
        [SerializeField] private bool _invertPanY = false;
        
        [Header("Edge Scrolling")]
        [SerializeField] private bool _enableEdgeScrolling = true;
        [SerializeField] private float _edgeScrollBorder = 15f;
        [SerializeField] private float _edgeScrollSpeed = 3.0f;
        
        [Header("Zoom Settings")]
        [SerializeField] private float _zoomSpeed = 2.0f;
        [SerializeField] private float _minZoom = 5.0f;
        [SerializeField] private float _maxZoom = 30.0f;
        [SerializeField] private float _zoomSmoothTime = 0.2f;
        [SerializeField] private bool _orthographicProjection = true;
        
        [Header("Rotation Settings")]
        [SerializeField] private bool _enableRotation = true;
        [SerializeField] private float _rotationSpeed = 50.0f;
        [SerializeField] private float _rotationSmoothTime = 0.3f;
        
        [Header("Boundaries")]
        [SerializeField] private bool _useBoundaries = true;
        [SerializeField] private Bounds _movementBounds = new Bounds(Vector3.zero, new Vector3(100, 10, 100));
        
        [Header("Height Settings")]
        [SerializeField] private float _baseHeight = 15.0f;
        [SerializeField] private float _heightOffset = 5.0f;
        [SerializeField] private AnimationCurve _heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private IPauseService _pauseService;
        private InputSettings_SO _inputSettings;
        private IEventSystem _eventSystem;
        
        // Input state
        private Vector2 _lookInput = Vector2.zero;
        private Vector2 _panInput = Vector2.zero;
        private Vector2 _edgeScrollInput = Vector2.zero;
        private float _rotationInput = 0f;
        private Vector2 _currentMousePosition = Vector2.zero;
        
        // Movement state
        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _targetVelocity = Vector3.zero;
        
        // Zoom state
        private float _currentZoom = 15.0f;
        private float _targetZoom = 15.0f;
        private float _zoomVelocity = 0f;
        
        // Rotation state
        private float _currentRotation = 0f;
        private float _targetRotation = 0f;
        private float _rotationVelocity = 0f;
        
        // Input processing
        private float _globalMouseSensitivity = 1.0f;
        private bool _inputEnabled = true;
        
        // Cinemachine components
        private CinemachinePositionComposer _positionComposer;
        
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
                Debug.LogError("[RTSCameraControl] CinemachineCamera is required but not assigned.");
                return;
            }
            
            // Create camera rig if not provided
            if (_cameraRig == null)
            {
                CreateCameraRig();
            }
            
            // Setup Cinemachine camera for RTS
            //SetupCinemachineCamera();
            
            // Initialize state
            _currentZoom = _minZoom + (_maxZoom - _minZoom) * 0.5f; // Start at middle zoom
            _targetZoom = _currentZoom;
            
            _isInitialized = true;
            
            // Subscribe to UI input events for RTS camera control
            if (_eventSystem != null)
            {
                _eventSystem.Subscribe<UIScrollWheelInputEvent>(OnScrollWheel);
                _eventSystem.Subscribe<UIPointInputEvent>(OnMousePosition);
                _eventSystem.Subscribe<UIMiddleClickInputEvent>(OnMiddleClick);
                _eventSystem.Subscribe<PlayerPreviousInputEvent>(OnRotateLeft); // Q key
                _eventSystem.Subscribe<PlayerNextInputEvent>(OnRotateRight); // E key
            }

            if (_showDebugInfo)
                Debug.Log("[RTSCameraControl] Initialized successfully");
        }

        public void Cleanup()
        {
            // Unsubscribe from events
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<UIScrollWheelInputEvent>(OnScrollWheel);
                _eventSystem.Unsubscribe<UIPointInputEvent>(OnMousePosition);
                _eventSystem.Unsubscribe<UIMiddleClickInputEvent>(OnMiddleClick);
                _eventSystem.Unsubscribe<PlayerPreviousInputEvent>(OnRotateLeft);
                _eventSystem.Unsubscribe<PlayerNextInputEvent>(OnRotateRight);
            }

            // Clear references
            _pauseService = null;
            _inputSettings = null;
            _eventSystem = null;
            _positionComposer = null;
            
            _isInitialized = false;
            
            if (_showDebugInfo)
                Debug.Log("[RTSCameraControl] Cleaned up");
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
            HandleEdgeScrolling();
            HandleRotation();
            UpdateMovement();
            UpdateZoom();
            UpdateHeight();
            
            // Reset input after processing
            _lookInput = Vector2.zero;
        }

        public void SetTarget(Transform target)
        {
            // RTS cameras typically don't follow a specific target
            // Could be used to focus on a specific unit or building
            if (target != null)
            {
                FocusOnTarget(target.position);
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
                _rotationInput = 0f;
            }
        }
        #endregion

        #region Private Methods
        private void CreateCameraRig()
        {
            GameObject rigObject = new GameObject("RTS_CameraRig");
            _cameraRig = rigObject.transform;
            _cameraRig.position = new Vector3(0, _baseHeight, 0);
            
            if (_showDebugInfo)
                Debug.Log("[RTSCameraControl] Created camera rig");
        }

        private void SetupCinemachineCamera()
        {
            if (_cinemachineCamera == null || _cameraRig == null) return;
            
            // Set camera rig as follow target
            _cinemachineCamera.Follow = _cameraRig;
            
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
            
            // Setup position composer for smooth following
            _positionComposer = _cinemachineCamera.GetComponent<CinemachinePositionComposer>();
            if (_positionComposer == null)
            {
                _positionComposer = _cinemachineCamera.gameObject.AddComponent<CinemachinePositionComposer>();
            }
            
            _positionComposer.Composition.ScreenPosition = new Vector2(0.5f, 0.5f); 
            //_positionComposer.Composition.HorizontalPosition = 0.5f;
            //_positionComposer.Composition.VerticalPosition = 0.5f;
            //_positionComposer.Composition.LookaheadTime = 0f; // No prediction for RTS
            //_positionComposer.Composition.LookaheadSmoothing = 0f;
            
            // Set RTS camera angle
            Vector3 cameraRotation = _cinemachineCamera.transform.localEulerAngles;
            cameraRotation.x = 45f; // Standard RTS angle
            _cinemachineCamera.transform.localEulerAngles = cameraRotation;
            
            if (_showDebugInfo)
                Debug.Log("[RTSCameraControl] Cinemachine camera configured for RTS");
        }

        private void ApplyInputSettings()
        {
            if (_inputSettings == null) return;
            
            _globalMouseSensitivity = _inputSettings.GetMouseSensitivity();
            
            if (_showDebugInfo)
            {
                Debug.Log($"[RTSCameraControl] Applied Input Settings - Global Sensitivity: {_globalMouseSensitivity}");
            }
        }

        private void ProcessInputs()
        {
            // Convert look input to pan input for RTS
            _panInput = _lookInput * EffectiveMouseSensitivity;
            
            // Apply inversions
            if (_invertPanX) _panInput.x *= -1f;
            if (_invertPanY) _panInput.y *= -1f;
        }

        private void HandleEdgeScrolling()
        {
            if (!_enableEdgeScrolling) return;
            
            Vector2 mousePosition = _currentMousePosition;
            _edgeScrollInput = Vector2.zero;
            
            // Check screen edges
            if (mousePosition.x <= _edgeScrollBorder)
                _edgeScrollInput.x = -1f;
            else if (mousePosition.x >= Screen.width - _edgeScrollBorder)
                _edgeScrollInput.x = 1f;
                
            if (mousePosition.y <= _edgeScrollBorder)
                _edgeScrollInput.y = -1f;
            else if (mousePosition.y >= Screen.height - _edgeScrollBorder)
                _edgeScrollInput.y = 1f;
        }

        private void OnScrollWheel(UIScrollWheelInputEvent scrollEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            float scrollInput = scrollEvent.ScrollDelta.y;
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                _targetZoom -= scrollInput * _zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }
        }

        private void UpdateZoom()
        {
            // Apply smooth zoom
            _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, _zoomSmoothTime);
            
            if (_cinemachineCamera.Lens.Orthographic)
            {
                _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
            }
            else
            {
                // For perspective, adjust height based on zoom
                UpdateHeight();
            }
        }

        private void HandleRotation()
        {
            if (!_enableRotation || Mathf.Abs(_rotationInput) < 0.01f) return;
            
            _targetRotation += _rotationInput * _rotationSpeed * Time.deltaTime;
            
            // Smooth rotation
            _currentRotation = Mathf.SmoothDampAngle(_currentRotation, _targetRotation, ref _rotationVelocity, _rotationSmoothTime);
            
            if (_cameraRig != null)
            {
                Vector3 rotation = _cameraRig.eulerAngles;
                rotation.y = _currentRotation;
                _cameraRig.rotation = Quaternion.Euler(rotation);
            }
        }

        private void UpdateMovement()
        {
            if (_cameraRig == null) return;
            
            // Combine pan input and edge scrolling
            Vector2 totalInput = _panInput + (_edgeScrollInput * _edgeScrollSpeed);
            
            // Convert to world space movement (consider camera rotation)
            Vector3 forward = _cameraRig.forward;
            Vector3 right = _cameraRig.right;
            
            // Flatten directions to horizontal plane
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            // Calculate target velocity
            Vector3 worldMovement = (right * totalInput.x + forward * totalInput.y) * _panSpeed;
            _targetVelocity = worldMovement;
            
            // Apply acceleration/deceleration
            if (_targetVelocity.magnitude > 0.01f)
            {
                _currentVelocity = Vector3.MoveTowards(_currentVelocity, _targetVelocity, _panAcceleration * Time.deltaTime);
            }
            else
            {
                _currentVelocity = Vector3.MoveTowards(_currentVelocity, Vector3.zero, _panDeceleration * Time.deltaTime);
            }
            
            // Apply movement with boundaries
            Vector3 newPosition = _cameraRig.position + _currentVelocity * Time.deltaTime;
            
            if (_useBoundaries)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, _movementBounds.min.x, _movementBounds.max.x);
                newPosition.z = Mathf.Clamp(newPosition.z, _movementBounds.min.z, _movementBounds.max.z);
            }
            
            _cameraRig.position = newPosition;
        }

        private void UpdateHeight()
        {
            if (_cameraRig == null) return;
            
            // Calculate height based on zoom level
            float zoomNormalized = (_currentZoom - _minZoom) / (_maxZoom - _minZoom);
            float heightMultiplier = _heightCurve.Evaluate(zoomNormalized);
            float targetHeight = _baseHeight + (_heightOffset * heightMultiplier);
            
            Vector3 position = _cameraRig.position;
            position.y = targetHeight;
            _cameraRig.position = position;
        }

        private void OnMousePosition(UIPointInputEvent pointEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            _currentMousePosition = pointEvent.Position;
        }

        private void OnMiddleClick(UIMiddleClickInputEvent middleClickEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            // Use middle mouse button for rotation
            _rotationInput = _lookInput.x * 0.5f;
        }

        private void OnRotateLeft(PlayerPreviousInputEvent rotateEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            _rotationInput = -1f;
        }

        private void OnRotateRight(PlayerNextInputEvent rotateEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            _rotationInput = 1f;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Focus the camera on a specific world position
        /// </summary>
        public void FocusOnTarget(Vector3 worldPosition)
        {
            if (_cameraRig == null) return;
            
            Vector3 newPosition = _cameraRig.position;
            newPosition.x = worldPosition.x;
            newPosition.z = worldPosition.z;
            
            if (_useBoundaries)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, _movementBounds.min.x, _movementBounds.max.x);
                newPosition.z = Mathf.Clamp(newPosition.z, _movementBounds.min.z, _movementBounds.max.z);
            }
            
            _cameraRig.position = newPosition;
            _currentVelocity = Vector3.zero;
        }
        
        /// <summary>
        /// Set the zoom level
        /// </summary>
        public void SetZoom(float zoom)
        {
            _targetZoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
        }
        
        /// <summary>
        /// Set the movement boundaries
        /// </summary>
        public void SetBoundaries(Bounds bounds)
        {
            _movementBounds = bounds;
        }
        
        /// <summary>
        /// Set the camera rotation
        /// </summary>
        public void SetRotation(float rotation)
        {
            _targetRotation = rotation;
        }
        
        /// <summary>
        /// Enable or disable edge scrolling
        /// </summary>
        public void SetEdgeScrolling(bool enabled)
        {
            _enableEdgeScrolling = enabled;
        }
        
        /// <summary>
        /// Get the current zoom level
        /// </summary>
        public float GetCurrentZoom()
        {
            return _currentZoom;
        }
        
        /// <summary>
        /// Get the current camera rotation
        /// </summary>
        public float GetCurrentRotation()
        {
            return _currentRotation;
        }
        #endregion

        #region Debug
        private void OnDrawGizmos()
        {
            if (!_showDebugInfo) return;
            
            // Draw movement boundaries
            if (_useBoundaries)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(_movementBounds.center, _movementBounds.size);
            }
            
            // Draw current velocity
            if (_cameraRig != null && _currentVelocity.magnitude > 0.01f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(_cameraRig.position, _currentVelocity);
            }
        }
        #endregion
    }
}
