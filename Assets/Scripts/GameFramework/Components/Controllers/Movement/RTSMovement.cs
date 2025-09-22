using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;

namespace GameFramework.Components.Controllers.Movement
{
    /// <summary>
    /// RTS movement component that handles camera movement for Real-Time Strategy games.
    /// Includes WASD camera panning, edge scrolling, zoom, and rotation.
    /// This component moves the camera itself rather than a character.
    /// </summary>
    public class RTSMovement : MonoBehaviour, IPlayerMovement
    {
        #region Serialized Fields
        [Header("Camera Movement")]
        [SerializeField] private float _panSpeed = 5.0f;
        [SerializeField] private float _panAcceleration = 2.0f;
        [SerializeField] private float _panDeceleration = 5.0f;
        [SerializeField] private bool _invertXAxis = false;
        [SerializeField] private bool _invertZAxis = false;
        
        [Header("Edge Scrolling")]
        [SerializeField] private bool _enableEdgeScrolling = true;
        [SerializeField] private float _edgeScrollBorder = 10f;
        [SerializeField] private float _edgeScrollSpeed = 3.0f;
        
        [Header("Zoom Settings")]
        [SerializeField] private float _zoomSpeed = 5.0f;
        [SerializeField] private float _minZoom = 3.0f;
        [SerializeField] private float _maxZoom = 20.0f;
        [SerializeField] private bool _smoothZoom = true;
        [SerializeField] private float _zoomSmoothTime = 0.2f;
        
        [Header("Boundaries")]
        [SerializeField] private bool _useBoundaries = true;
        [SerializeField] private Vector2 _minBounds = new Vector2(-50, -50);
        [SerializeField] private Vector2 _maxBounds = new Vector2(50, 50);
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private UnityEngine.Camera _camera;
        private IPauseService _pauseService;
        
        // Movement state
        private Vector2 _moveInput = Vector2.zero;
        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _targetVelocity = Vector3.zero;
        
        // Zoom state
        private float _currentZoom = 10.0f;
        private float _targetZoom = 10.0f;
        private float _zoomVelocity = 0f;
        
        // Edge scrolling
        private Vector2 _mousePosition = Vector2.zero;
        private Vector2 _edgeScrollInput = Vector2.zero;
        
        // Component state
        private bool _isInitialized = false;
        #endregion

        #region Public Properties
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        public Transform MovementTransform => transform;
        public Vector3 CurrentVelocity => _currentVelocity;
        public bool IsGrounded => true; // Always "grounded" for RTS camera
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get camera component
            _camera = GetComponent<UnityEngine.Camera>();
            
            // Get services
            _pauseService = GameManager.GetService<IPauseService>();
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            UpdateMovement();
        }
        #endregion

        #region IPlayerMovement Implementation
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_camera == null)
            {
                _camera = GameManager.GetService<IGameDataService>().GetMainCamera(); 
            }
            
            // Initialize zoom
            _currentZoom = _camera.orthographic ? _camera.orthographicSize : transform.position.y;
            _targetZoom = _currentZoom;
            
            _isInitialized = true;
            
            if (_showDebugInfo)
                Debug.Log($"[RTSMovement] Initialized on {gameObject.name}");
        }

        public void Cleanup()
        {
            _pauseService = null;
            _camera = null;
            _isInitialized = false;
            
            if (_showDebugInfo)
                Debug.Log($"[RTSMovement] Cleaned up on {gameObject.name}");
        }

        public void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            _moveInput = inputEvent.MovementVector;
            
            // Apply axis inversions
            if (_invertXAxis) _moveInput.x *= -1;
            if (_invertZAxis) _moveInput.y *= -1;
        }

        public void HandleJumpInput(PlayerJumpInputEvent inputEvent)
        {
            // Not applicable for RTS movement
        }

        public void HandleSprintInput(PlayerSprintInputEvent inputEvent)
        {
            // Could be used to increase pan speed, but not implemented here
        }

        public void HandleCrouchInput(PlayerCrouchInputEvent inputEvent)
        {
            // Not applicable for RTS movement
        }

        public void UpdateMovement()
        {
            if (!_isInitialized || IsPaused) return;
            
            CalculateTargetVelocity();
            UpdateCameraPosition();
        }

        public void FixedUpdateMovement()
        {
            // RTS camera movement is handled in Update for responsiveness
        }

        public void StopMovement()
        {
            _moveInput = Vector2.zero;
            _edgeScrollInput = Vector2.zero;
            _currentVelocity = Vector3.zero;
            _targetVelocity = Vector3.zero;
        }
        #endregion

        #region Private Methods
        private void HandleEdgeScrolling()
        {
            if (!_enableEdgeScrolling) return;
            
            // Edge scrolling is now handled by the RTSCameraControl class
            // This method is kept for interface compatibility
        }

        private void HandleZoom()
        {
            // Zoom is now handled by the RTSCameraControl class
            // This legacy code is replaced by event-driven input
            
            // Apply zoom
            if (_smoothZoom)
            {
                _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, _zoomSmoothTime);
            }
            else
            {
                _currentZoom = _targetZoom;
            }
            
            // Set camera zoom based on projection type
            if (_camera.orthographic)
            {
                _camera.orthographicSize = _currentZoom;
            }
            else
            {
                Vector3 position = transform.position;
                position.y = _currentZoom;
                transform.position = position;
            }
        }

        private void CalculateTargetVelocity()
        {
            // Combine WASD input and edge scrolling
            Vector2 totalInput = _moveInput + (_edgeScrollInput * _edgeScrollSpeed / _panSpeed);
            
            // Convert to 3D movement (XZ plane)
            Vector3 inputDirection = new Vector3(totalInput.x, 0f, totalInput.y);
            
            // Calculate target velocity
            _targetVelocity = inputDirection * _panSpeed;
        }

        private void UpdateCameraPosition()
        {
            if (_targetVelocity.magnitude > 0.01f)
            {
                // Accelerate towards target velocity
                _currentVelocity = Vector3.MoveTowards(
                    _currentVelocity,
                    _targetVelocity,
                    _panAcceleration * Time.deltaTime
                );
            }
            else
            {
                // Decelerate when no input
                _currentVelocity = Vector3.MoveTowards(
                    _currentVelocity,
                    Vector3.zero,
                    _panDeceleration * Time.deltaTime
                );
            }
            
            // Apply movement
            Vector3 newPosition = transform.position + _currentVelocity * Time.deltaTime;
            
            // Apply boundaries if enabled
            if (_useBoundaries)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, _minBounds.x, _maxBounds.x);
                newPosition.z = Mathf.Clamp(newPosition.z, _minBounds.y, _maxBounds.y);
            }
            
            transform.position = newPosition;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the camera zoom level
        /// </summary>
        public void SetZoom(float zoom)
        {
            _targetZoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
        }
        
        /// <summary>
        /// Set the movement boundaries for the camera
        /// </summary>
        public void SetBoundaries(Vector2 minBounds, Vector2 maxBounds)
        {
            _minBounds = minBounds;
            _maxBounds = maxBounds;
        }
        
        /// <summary>
        /// Enable or disable edge scrolling
        /// </summary>
        public void SetEdgeScrolling(bool enabled)
        {
            _enableEdgeScrolling = enabled;
        }
        
        /// <summary>
        /// Focus the camera on a specific world position
        /// </summary>
        public void FocusOn(Vector3 worldPosition)
        {
            Vector3 newPosition = transform.position;
            newPosition.x = worldPosition.x;
            newPosition.z = worldPosition.z;
            
            if (_useBoundaries)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, _minBounds.x, _maxBounds.x);
                newPosition.z = Mathf.Clamp(newPosition.z, _minBounds.y, _maxBounds.y);
            }
            
            transform.position = newPosition;
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
                    transform.position.y,
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
            if (_currentVelocity.magnitude > 0.01f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, _currentVelocity);
            }
        }
        #endregion
    }
}
