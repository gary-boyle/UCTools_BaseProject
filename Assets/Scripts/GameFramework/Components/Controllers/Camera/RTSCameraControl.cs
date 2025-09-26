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
    public class RTSCameraControl : BaseCameraComponent
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
        
        [Header("Optional Rotation")]
        [SerializeField] private bool _enableRotation = false;
        [SerializeField] private float _rotationSpeed = 60.0f;
        [SerializeField] private float _rotationSmoothness = 5.0f;
        
        [Header("Movement Boundaries")]
        [SerializeField] private bool _useBoundaries = true;
        [SerializeField] private Bounds _moveBounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
        #endregion

        #region RTS Camera Specific Fields
        // Movement state
        private Vector2 _moveInput = Vector2.zero;
        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _targetVelocity = Vector3.zero;
        
        // Rotation state
        private float _currentRotation = 0f;
        private float _targetRotation = 0f;
        private bool _isRotating = false;
        
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            // Create camera rig if not provided
            if (_cameraRig == null)
            {
                Debug.LogError("No CameraRig supplied");
            }
        }
        #endregion

        #region BaseCameraComponent Implementation
        protected override void InitializeCameraSpecific()
        {
            if (_cinemachineCamera == null)
            {
                Debug.LogError("[RTSCameraControl] CinemachineCamera is required but not assigned.");
                return;
            }
            
            // Initialize zoom
            _currentZoom = (_minZoom + _maxZoom) / 2f;
            _targetZoom = _currentZoom;
            
            // Set up camera projection
            var mainCamera = GameManager.GetService<IGameDataService>().GetMainCamera();

            if (mainCamera == null) return;
            mainCamera.orthographic = _orthographicProjection;
            if (_orthographicProjection)
            {
                mainCamera.orthographicSize = _currentZoom;
            }
        }

        protected override void CleanupCameraSpecific()
        {
        }

        protected override void ProcessLookInput()
        {
            // RTS cameras typically don't use look input for rotation unless middle mouse button is held
            // This could be extended for middle mouse button rotation if needed
        }

        protected override void UpdateCameraSpecific()
        {
            UpdateMovement();
            UpdateEdgeScrolling();
            UpdateRotation();
        }
        #endregion
        
        #region RTS Specific Methods
        public void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused || !_inputEnabled) return;
            
            _moveInput = inputEvent.MovementVector;
            
            // Apply axis inversions
            if (_invertXAxis) _moveInput.x *= -1f;
            if (_invertYAxis) _moveInput.y *= -1f;
        }
        
        public void FocusOnPosition(Vector3 position)
        {
            if (_cameraRig != null)
            {
                Vector3 targetPosition = new Vector3(position.x, _cameraRig.position.y, position.z);
                
                // Apply boundaries if enabled
                if (_useBoundaries)
                {
                    targetPosition = ConstrainToBounds(targetPosition);
                }
                
                _cameraRig.position = targetPosition;
            }
        }
        
        public Transform GetCameraTransform()
        {
            return _cinemachineCamera?.transform;
        }
        #endregion

        #region Private Methods
        private void UpdateMovement()
        {
            if (_moveInput.magnitude < 0.01f && _currentVelocity.magnitude < 0.01f) return;
            
            // Calculate target velocity
            Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
            _targetVelocity = moveDirection * _moveSpeed;
            
            // Smooth velocity changes
            if (_moveInput.magnitude > 0.01f)
            {
                _currentVelocity = Vector3.MoveTowards(_currentVelocity, _targetVelocity, 
                    _moveAcceleration * Time.deltaTime);
            }
            else
            {
                _currentVelocity = Vector3.MoveTowards(_currentVelocity, Vector3.zero, 
                    _moveDeceleration * Time.deltaTime);
            }
            
            // Apply movement
            if (_currentVelocity.magnitude > 0.01f)
            {
                Vector3 newPosition = _cameraRig.position + _currentVelocity * Time.deltaTime;
                
                // Apply boundaries if enabled
                if (_useBoundaries)
                {
                    newPosition = ConstrainToBounds(newPosition);
                }
                
                _cameraRig.position = newPosition;
            }
        }
        
        private void UpdateEdgeScrolling()
        {
            if (!_enableEdgeScrolling || _mainCamera == null) return;
            
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            
            Vector2 edgeInput = Vector2.zero;
            
            // Check screen edges
            if (mousePosition.x <= _edgeScrollBorder)
                edgeInput.x = -1f;
            else if (mousePosition.x >= screenSize.x - _edgeScrollBorder)
                edgeInput.x = 1f;
                
            if (mousePosition.y <= _edgeScrollBorder)
                edgeInput.y = -1f;
            else if (mousePosition.y >= screenSize.y - _edgeScrollBorder)
                edgeInput.y = 1f;
            
            // Apply edge scrolling
            if (edgeInput.magnitude > 0.01f)
            {
                Vector3 edgeMovement = new Vector3(edgeInput.x, 0f, edgeInput.y) * (_edgeScrollSpeed * Time.deltaTime);
                Vector3 newPosition = _cameraRig.position + edgeMovement;
                
                // Apply boundaries if enabled
                if (_useBoundaries)
                {
                    newPosition = ConstrainToBounds(newPosition);
                }
                
                _cameraRig.position = newPosition;
            }
        }

        private void UpdateRotation()
        {
            if (!_enableRotation) return;
            
            if (Mathf.Abs(_currentRotation - _targetRotation) > 0.01f)
            {
                _currentRotation = Mathf.Lerp(_currentRotation, _targetRotation, _rotationSmoothness * Time.deltaTime);
                
                if (_cameraRig != null)
                {
                    _cameraRig.rotation = Quaternion.Euler(0f, _currentRotation, 0f);
                }
            }
        }
        
        private Vector3 ConstrainToBounds(Vector3 position)
        {
            if (!_useBoundaries) return position;
            
            position.x = Mathf.Clamp(position.x, _moveBounds.min.x, _moveBounds.max.x);
            position.z = Mathf.Clamp(position.z, _moveBounds.min.z, _moveBounds.max.z);
            
            return position;
        }
        
        protected override void UpdateZoom()
        {
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, _zoomSmoothness * Time.deltaTime);
            
            if (!(Mathf.Abs(_currentZoom - _targetZoom) > 0.01f)) return;
            
            if (_orthographicProjection)
            {
                _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
            }
            else
            {
                // Apply zoom to camera rig
                var position = _cameraRig.transform.position;
                position = new Vector3(position.x, _currentZoom, position.z);
                _cameraRig.position = position;
            }
        }
        
        #endregion

        #region Debug
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            if (!_showDebugInfo) return;
            
            // Draw movement boundaries
            if (_useBoundaries)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(_moveBounds.center, _moveBounds.size);
            }
        }
        #endregion
    }
}