using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.Config.ScriptableObjects;
using GameFramework.EventSystem.Interfaces;
using UnityEngine.InputSystem;

namespace GameFramework.Components.Controllers.Camera
{
    /// <summary>
    /// Complete RTS camera control system.
    /// Handles WASD movement, edge scrolling, mouse wheel zooming, and optional middle mouse button rotation.
    /// All camera functionality is consolidated in this single component.
    /// </summary>
    public class RTSCameraControl : MonoBehaviour, ICameraControl
    {
        #region Serialized Fields
        [Header("Camera Setup")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraRig; // Optional - will create if not provided
        [SerializeField] private bool _orthographicProjection = true;
        
        [Header("WASD Movement")]
        [SerializeField] private float _moveSpeed = 8.0f;
        [SerializeField] private float _moveAcceleration = 15.0f;
        [SerializeField] private float _moveDeceleration = 15.0f;
        [SerializeField] private bool _invertXAxis = false;
        [SerializeField] private bool _invertYAxis = false;
        
        [Header("Edge Scrolling")]
        [SerializeField] private bool _enableEdgeScrolling = true;
        [SerializeField] private float _edgeScrollBorder = 20f;
        [SerializeField] private float _edgeScrollSpeed = 6.0f;
        
        [Header("Mouse Zoom")]
        [SerializeField] private float _zoomSpeed = 3.0f;
        [SerializeField] private float _minZoom = 5.0f;
        [SerializeField] private float _maxZoom = 25.0f;
        [SerializeField] private float _zoomSmoothness = 8.0f;
        
        [Header("Optional Rotation")]
        [SerializeField] private bool _enableRotation = false;
        [SerializeField] private float _rotationSpeed = 60.0f;
        [SerializeField] private float _rotationSmoothness = 5.0f;
        
        [Header("Movement Boundaries")]
        [SerializeField] private bool _useBoundaries = true;
        [SerializeField] private Vector2 _minBounds = new Vector2(-50, -50);
        [SerializeField] private Vector2 _maxBounds = new Vector2(50, 50);
        
        [Header("Camera Height")]
        [SerializeField] private float _cameraHeight = 15.0f;
        [SerializeField] private float _cameraAngle = 45.0f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private IPauseService _pauseService;
        private InputSettings_SO _inputSettings;
        private IEventSystem _eventSystem;
        private UnityEngine.Camera _mainCamera;
        
        // Input state
        private Vector2 _moveInput = Vector2.zero;
        private Vector2 _edgeScrollInput = Vector2.zero;
        private Vector2 _currentMousePosition = Vector2.zero;
        private Vector2 _mouseDelta = Vector2.zero;
        
        // Input processing
        private float _globalMouseSensitivity = 1.0f;
        private float _mouseSensitivityMultiplier = 1.0f;
        
        // Movement state
        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _targetVelocity = Vector3.zero;
        
        // Zoom state
        private float _currentZoom = 15.0f;
        private float _targetZoom = 15.0f;
        
        // Rotation state (if enabled)
        private float _currentRotation = 0f;
        private float _targetRotation = 0f;
        
        // Component state
        private bool _isInitialized = false;
        private bool _inputEnabled = true;
        #endregion

        #region Public Properties
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        
        public float MouseSensitivityMultiplier 
        { 
            get => _mouseSensitivityMultiplier; 
            set => _mouseSensitivityMultiplier = Mathf.Clamp(value, 0.001f, 2.0f); 
        }
        public float EffectiveMouseSensitivity => _globalMouseSensitivity * _mouseSensitivityMultiplier;

        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get services
            _pauseService = GameManager.GetService<IPauseService>();
            _eventSystem = GameManager.GetService<IEventSystem>();
            _inputSettings = SettingsRegistry.Get<InputSettings_SO>();
            
            // Get main camera reference
            _mainCamera = GameManager.GetService<IGameDataService>().GetMainCamera();
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
            
            // Initialize zoom to middle range
            _currentZoom = (_minZoom + _maxZoom) * 0.5f;
            _targetZoom = _currentZoom;
            
            SetupCinemachineCamera();
            
            // Apply input settings for mouse sensitivity
            ApplyInputSettings();
            
            // Subscribe to input events
            if (_eventSystem != null)
            {
                _eventSystem.Subscribe<UIScrollWheelInputEvent>(OnScrollWheel);
                _eventSystem.Subscribe<UIPointInputEvent>(OnMousePosition);
                _eventSystem.Subscribe<PlayerLookInputEvent>(OnMouseLook);
                _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);
            }
            
            _isInitialized = true;
        }

        public void Cleanup()
        {
            // Unsubscribe from events
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<UIScrollWheelInputEvent>(OnScrollWheel);
                _eventSystem.Unsubscribe<UIPointInputEvent>(OnMousePosition);
                _eventSystem.Unsubscribe<PlayerLookInputEvent>(OnMouseLook);
                _eventSystem.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
            }

            // Clear references
            _pauseService = null;
            _inputSettings = null;
            _eventSystem = null;
            _mainCamera = null;
            
            _isInitialized = false;
        }

        private void SetupCinemachineCamera()
        {
            if (_cinemachineCamera == null || _cameraRig == null) return;
            
            // Set camera rig as follow target
            _cinemachineCamera.Follow = _cameraRig;
            
            // Configure camera projection and angle
            if (_mainCamera != null)
            {
                _mainCamera.orthographic = _orthographicProjection;
                
                if (_orthographicProjection)
                {
                    _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
                }
                else
                {
                    _cinemachineCamera.Lens.FieldOfView = 60f;
                }
            }
            
            // Set RTS camera angle
            Vector3 cameraRotation = _cinemachineCamera.transform.localEulerAngles;
            cameraRotation.x = _cameraAngle;
            _cinemachineCamera.transform.localEulerAngles = cameraRotation;
        }
        
        /// <summary>
        /// Handle WASD movement input for camera panning
        /// </summary>
        public void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            _moveInput = inputEvent.MovementVector;
            
            // Apply axis inversions
            if (_invertXAxis) _moveInput.x *= -1f;
            if (_invertYAxis) _moveInput.y *= -1f;
        }

        public void HandleLookInput(PlayerLookInputEvent inputEvent)
        {
            // Not used in RTS - camera doesn't look around with mouse
        }

        public void UpdateCamera()
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            HandleEdgeScrolling();
            UpdateMovement();
            UpdateZoom();
            
            if (_enableRotation)
            {
                HandleRotation();
            }
            
            UpdateCameraHeight();
        }
        
        public void SetTarget(Transform target)
        {
            // RTS cameras typically don't follow a specific target
            // But can be used to focus on a specific unit or building
            if (target != null)
            {
                FocusOnPosition(target.position);
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
                _mouseDelta = Vector2.zero;
            }
        }
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Apply input settings from the global InputSettings_SO for mouse sensitivity
        /// </summary>
        private void ApplyInputSettings()
        {
            if (_inputSettings == null) return;
            
            _globalMouseSensitivity = _inputSettings.GetMouseSensitivity();
        }
        
        /// <summary>
        /// Handle edge scrolling when mouse is near screen edges
        /// </summary>

        private void HandleEdgeScrolling()
        {
            if (!_enableEdgeScrolling) 
            {
                _edgeScrollInput = Vector2.zero;
                return;
            }
            
            Vector2 mousePos = _currentMousePosition;
            _edgeScrollInput = Vector2.zero;
            
            // Check each screen edge
            if (mousePos.x <= _edgeScrollBorder)
                _edgeScrollInput.x = -1f;
            else if (mousePos.x >= Screen.width - _edgeScrollBorder)
                _edgeScrollInput.x = 1f;
                
            if (mousePos.y <= _edgeScrollBorder)
                _edgeScrollInput.y = -1f;
            else if (mousePos.y >= Screen.height - _edgeScrollBorder)
                _edgeScrollInput.y = 1f;
        }

        /// <summary>
        /// Update camera movement based on WASD input and edge scrolling
        /// </summary>
        private void UpdateMovement()
        {
            if (_cameraRig == null) return;
            
            // Combine WASD and edge scroll input
            Vector2 totalInput = _moveInput + (_edgeScrollInput * _edgeScrollSpeed / _moveSpeed);
            
            // Convert to world movement (considering camera rotation if enabled)
            Vector3 worldMovement;
            if (_enableRotation)
            {
                Vector3 forward = _cameraRig.forward;
                Vector3 right = _cameraRig.right;
                forward.y = 0f; right.y = 0f;
                forward.Normalize(); right.Normalize();
                worldMovement = (right * totalInput.x + forward * totalInput.y) * _moveSpeed;
            }
            else
            {
                // Simple XZ plane movement
                worldMovement = new Vector3(totalInput.x, 0f, totalInput.y) * _moveSpeed;
            }
            
            _targetVelocity = worldMovement;
            
            // Apply acceleration/deceleration
            if (_targetVelocity.magnitude > 0.01f)
            {
                _currentVelocity = Vector3.MoveTowards(_currentVelocity, _targetVelocity, _moveAcceleration * Time.deltaTime);
            }
            else
            {
                _currentVelocity = Vector3.MoveTowards(_currentVelocity, Vector3.zero, _moveDeceleration * Time.deltaTime);
            }
            
            // Apply movement with boundaries
            Vector3 newPosition = _cameraRig.position + _currentVelocity * Time.deltaTime;
            
            if (_useBoundaries)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, _minBounds.x, _maxBounds.x);
                newPosition.z = Mathf.Clamp(newPosition.z, _minBounds.y, _maxBounds.y);
            }
            
            _cameraRig.position = newPosition;
        }

        /// <summary>
        /// Update camera zoom level
        /// </summary>
        private void UpdateZoom()
        {
            // Smooth zoom transition
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, _zoomSmoothness * Time.deltaTime);
            
            // Apply zoom based on projection type
            if (_cinemachineCamera != null)
            {
                if (_orthographicProjection)
                {
                    _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
                }
                else
                {
                    // For perspective cameras, adjust the camera height based on zoom level
                    // This is handled in UpdateCameraHeight() which is called every frame
                }
            }
        }

        /// <summary>
        /// Handle camera rotation (if enabled) using middle mouse button and mouse movement
        /// </summary>
        private void HandleRotation()
        {
            if (!_enableRotation || _cameraRig == null) return;
            // Only rotate when middle mouse button is held down
            if (Mouse.current.middleButton.IsPressed() && _mouseDelta.magnitude > 0.01f)
            {
                // Use horizontal mouse movement for rotation with mouse sensitivity applied
                float rotationDelta = _mouseDelta.x * EffectiveMouseSensitivity * _rotationSpeed * Time.deltaTime;
                _targetRotation += rotationDelta;
            }
            
            // Smooth rotation
            _currentRotation = Mathf.LerpAngle(_currentRotation, _targetRotation, _rotationSmoothness * Time.deltaTime);
            
            Vector3 rotation = _cameraRig.eulerAngles;
            rotation.y = _currentRotation;
            _cameraRig.rotation = Quaternion.Euler(rotation);
        }


        /// <summary>
        /// Update camera height - maintains constant height for orthographic, adjusts based on zoom for perspective
        /// </summary>
        private void UpdateCameraHeight()
        {
            if (_cameraRig == null) return;
            
            Vector3 position = _cameraRig.position;
            
            if (_orthographicProjection)
            {
                // For orthographic cameras, maintain constant height
                position.y = _cameraHeight;
            }
            else
            {
                // For perspective cameras, adjust height based on zoom level
                // Higher zoom values = higher camera position (more zoomed out)
                // Lower zoom values = lower camera position (more zoomed in)
                float zoomMultiplier = _currentZoom / ((_minZoom + _maxZoom) * 0.5f); // Normalize around middle zoom
                position.y = _cameraHeight * zoomMultiplier;
                
                // Ensure minimum height to prevent camera going underground
                position.y = Mathf.Max(position.y, 2.0f);
            }
            
            _cameraRig.position = position;
        }

        /// <summary>
        /// Handle mouse scroll wheel for zooming
        /// </summary>
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

        /// <summary>
        /// Track mouse position for edge scrolling
        /// </summary>
        private void OnMousePosition(UIPointInputEvent pointEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            _currentMousePosition = pointEvent.Position;
        }
        
        /// <summary>
        /// Handle mouse look input for rotation when middle mouse is pressed
        /// </summary>
        private void OnMouseLook(PlayerLookInputEvent lookEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            _mouseDelta = lookEvent.LookDelta;
        }
        
        /// <summary>
        /// Handle options changed event to update mouse sensitivity
        /// </summary>
        private void OnOptionsChanged(OptionsChangedEvent optionsEvent)
        {
            ApplyInputSettings();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Focus the camera on a specific world position
        /// </summary>
        public void FocusOnPosition(Vector3 worldPosition)
        {
            if (_cameraRig == null) return;
            
            Vector3 newPosition = _cameraRig.position;
            newPosition.x = worldPosition.x;
            newPosition.z = worldPosition.z;
            
            // Apply boundaries if enabled
            if (_useBoundaries)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, _minBounds.x, _maxBounds.x);
                newPosition.z = Mathf.Clamp(newPosition.z, _minBounds.y, _maxBounds.y);
            }
            
            _cameraRig.position = newPosition;
            _currentVelocity = Vector3.zero;
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
                Vector3 center = new Vector3(
                    (_minBounds.x + _maxBounds.x) * 0.5f,
                    _cameraHeight,
                    (_minBounds.y + _maxBounds.y) * 0.5f
                );
                Vector3 size = new Vector3(
                    _maxBounds.x - _minBounds.x,
                    0.1f,
                    _maxBounds.y - _minBounds.y
                );
                Gizmos.DrawWireCube(center, size);
            }
            
            // Draw current velocity
            if (_cameraRig != null && _currentVelocity.magnitude > 0.01f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(_cameraRig.position, _currentVelocity);
            }
            
            // Show edge scroll detection area
            if (_enableEdgeScrolling && _currentMousePosition != Vector2.zero)
            {
                Gizmos.color = Color.red;
                
                // Draw edge scroll borders (approximation in world space)
                if (_currentMousePosition.x <= _edgeScrollBorder || 
                    _currentMousePosition.x >= Screen.width - _edgeScrollBorder ||
                    _currentMousePosition.y <= _edgeScrollBorder || 
                    _currentMousePosition.y >= Screen.height - _edgeScrollBorder)
                {
                    if (_cameraRig != null)
                    {
                        Gizmos.DrawWireSphere(_cameraRig.position, 1f);
                    }
                }
            }
        }
        #endregion
    }
}
