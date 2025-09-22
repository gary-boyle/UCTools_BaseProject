using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using UnityEngine.InputSystem;

namespace GameFramework.Components.Controllers.Movement
{
    /// <summary>
    /// Isometric movement component for top-down and isometric view games.
    /// Handles 8-directional movement with optional diagonal speed normalization.
    /// Includes smooth rotation and animation support.
    /// </summary>
    public class IsometricMovement : MonoBehaviour, IPlayerMovement
    {
        #region Serialized Fields
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5.0f;
        [SerializeField] private float _sprintMultiplier = 1.5f;
        [SerializeField] private float _crouchMultiplier = 0.5f;
        [SerializeField] private bool _normalizeDiagonalMovement = true;
        
        [Header("Rotation Settings")]
        [SerializeField] private bool _rotateTowardsMovement = true;
        [SerializeField] private float _rotationSpeed = 10.0f;
        [SerializeField] private float _rotationSmoothTime = 0.1f;
        [SerializeField] private bool _snapRotation = false;
        [SerializeField] private float _snapAngleThreshold = 45f;
        
        [Header("Physics Settings")]
        [SerializeField] private bool _usePhysics = true;
        [SerializeField] private float _acceleration = 10.0f;
        [SerializeField] private float _deceleration = 10.0f;
        
        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayerMask = 1;
        [SerializeField] private float _groundCheckDistance = 0.1f;
        
        [Header("Crouching")]
        [SerializeField] private float _crouchHeight = 1.0f;
        [SerializeField] private float _standingHeight = 2.0f;
        [SerializeField] private float _crouchTransitionSpeed = 5.0f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;
        private IPauseService _pauseService;
        
        // Movement state
        private Vector2 _moveInput = Vector2.zero;
        private Vector3 _moveDirection = Vector3.zero;
        private Vector3 _currentVelocity = Vector3.zero;
        private bool _isSprinting = false;
        private bool _isCrouching = false;
        
        // Rotation state
        private float _targetRotation = 0f;
        private float _rotationVelocity = 0f;
        
        // Ground detection
        private bool _isGrounded = false;
        private Transform _groundCheckPoint;
        
        // Component state
        private bool _isInitialized = false;
        
        // 8-directional movement angles (in degrees)
        private readonly float[] _cardinalAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
        #endregion

        #region Public Properties
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        public Transform MovementTransform => transform;
        public Vector3 CurrentVelocity => _usePhysics && _rigidbody != null ? _rigidbody.linearVelocity : _currentVelocity;
        public bool IsGrounded => _isGrounded;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get required components
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();
            
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

        private void FixedUpdate()
        {
            FixedUpdateMovement();
        }
        #endregion

        #region IPlayerMovement Implementation
        public void Initialize()
        {
            if (_isInitialized) return;

            // Configure rigidbody for isometric movement if using physics
            if (_usePhysics && _rigidbody != null)
            {
                _rigidbody.freezeRotation = true;
                _rigidbody.useGravity = false; // Typically no gravity in isometric games
            }
            
            if (_collider == null && _usePhysics)
            {
                Debug.LogWarning($"[IsometricMovement] CapsuleCollider recommended for physics-based isometric movement on {gameObject.name}");
            }

            // Setup ground check
            SetupGroundCheck();
            
            _isInitialized = true;
            
            if (_showDebugInfo)
                Debug.Log($"[IsometricMovement] Initialized on {gameObject.name} (Physics: {_usePhysics})");
        }

        public void Cleanup()
        {
            _pauseService = null;
            _isInitialized = false;
            
            if (_showDebugInfo)
                Debug.Log($"[IsometricMovement] Cleaned up on {gameObject.name}");
        }

        public void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            _moveInput = inputEvent.MovementVector;
        }

        public void HandleJumpInput(PlayerJumpInputEvent inputEvent)
        {
            // Jump not typically used in isometric games, but could trigger special abilities
        }

        public void HandleSprintInput(PlayerSprintInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            _isSprinting = inputEvent.Phase == InputActionPhase.Performed;
        }

        public void HandleCrouchInput(PlayerCrouchInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            _isCrouching = inputEvent.Phase == InputActionPhase.Performed;
        }

        public void UpdateMovement()
        {
            if (!_isInitialized || IsPaused) return;
            
            CheckGrounded();
            HandleCrouchTransition();
            CalculateMovementDirection();
            HandleRotation();
            
            if (!_usePhysics)
            {
                HandleNonPhysicsMovement();
            }
        }

        public void FixedUpdateMovement()
        {
            if (!_isInitialized || IsPaused) return;
            
            if (_usePhysics)
            {
                HandlePhysicsMovement();
            }
        }

        public void StopMovement()
        {
            _moveInput = Vector2.zero;
            _moveDirection = Vector3.zero;
            _currentVelocity = Vector3.zero;
            
            if (_usePhysics && _rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
            }
        }
        #endregion

        #region Private Methods
        private void SetupGroundCheck()
        {
            if (_collider == null) return;
            
            // Create ground check point if it doesn't exist
            _groundCheckPoint = transform.Find("GroundCheckPoint");
            if (_groundCheckPoint == null)
            {
                GameObject groundCheck = new GameObject("GroundCheckPoint");
                groundCheck.transform.SetParent(transform);
                groundCheck.transform.localPosition = new Vector3(0, -_collider.bounds.extents.y, 0);
                _groundCheckPoint = groundCheck.transform;
            }
        }

        private void CheckGrounded()
        {
            if (_groundCheckPoint == null)
            {
                _isGrounded = true; // Assume grounded if no ground check
                return;
            }
            
            _isGrounded = Physics.Raycast(
                _groundCheckPoint.position,
                Vector3.down,
                _groundCheckDistance,
                _groundLayerMask
            );
        }

        private void CalculateMovementDirection()
        {
            if (_moveInput.magnitude < 0.01f)
            {
                _moveDirection = Vector3.zero;
                return;
            }
            
            // Convert 2D input to 3D direction (isometric typically uses XZ plane)
            Vector3 inputDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
            
            // Normalize diagonal movement if enabled
            if (_normalizeDiagonalMovement)
            {
                inputDirection.Normalize();
            }
            
            _moveDirection = inputDirection;
        }

        private void HandleRotation()
        {
            if (!_rotateTowardsMovement || _moveDirection.magnitude < 0.01f) return;
            
            // Calculate target rotation
            float targetAngle = Mathf.Atan2(_moveDirection.x, _moveDirection.z) * Mathf.Rad2Deg;
            
            // Snap to cardinal directions if enabled
            if (_snapRotation)
            {
                targetAngle = GetNearestCardinalAngle(targetAngle);
            }
            
            _targetRotation = targetAngle;
            
            // Apply rotation
            if (_snapRotation && Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, _targetRotation)) < _snapAngleThreshold)
            {
                // Instant rotation for snapping
                transform.rotation = Quaternion.Euler(0f, _targetRotation, 0f);
            }
            else
            {
                // Smooth rotation
                float currentYRotation = transform.eulerAngles.y;
                float smoothedRotation = Mathf.SmoothDampAngle(
                    currentYRotation,
                    _targetRotation,
                    ref _rotationVelocity,
                    _rotationSmoothTime
                );
                
                transform.rotation = Quaternion.Euler(0f, smoothedRotation, 0f);
            }
        }

        private void HandlePhysicsMovement()
        {
            if (_rigidbody == null) return;
            
            // Calculate target velocity
            Vector3 targetVelocity = CalculateTargetVelocity();
            
            if (targetVelocity.magnitude < 0.01f)
            {
                // Decelerate when no input
                Vector3 currentVel = _rigidbody.linearVelocity;
                Vector3 decelerationForce = -currentVel.normalized * _deceleration * Time.fixedDeltaTime;
                
                if (decelerationForce.magnitude > currentVel.magnitude)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                }
                else
                {
                    _rigidbody.linearVelocity += decelerationForce;
                }
            }
            else
            {
                // Accelerate towards target velocity
                Vector3 velocityDifference = targetVelocity - _rigidbody.linearVelocity;
                Vector3 accelerationForce = velocityDifference.normalized * _acceleration * Time.fixedDeltaTime;
                
                if (accelerationForce.magnitude > velocityDifference.magnitude)
                {
                    _rigidbody.linearVelocity = targetVelocity;
                }
                else
                {
                    _rigidbody.linearVelocity += accelerationForce;
                }
            }
        }

        private void HandleNonPhysicsMovement()
        {
            // Calculate target velocity
            Vector3 targetVelocity = CalculateTargetVelocity();
            
            if (targetVelocity.magnitude < 0.01f)
            {
                // Decelerate when no input
                _currentVelocity = Vector3.MoveTowards(_currentVelocity, Vector3.zero, _deceleration * Time.deltaTime);
            }
            else
            {
                // Accelerate towards target velocity
                _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, _acceleration * Time.deltaTime);
            }
            
            // Apply movement
            transform.position += _currentVelocity * Time.deltaTime;
        }

        private Vector3 CalculateTargetVelocity()
        {
            if (_moveDirection.magnitude < 0.01f) return Vector3.zero;
            
            // Calculate movement speed with modifiers
            float currentSpeed = _moveSpeed;
            if (_isSprinting && !_isCrouching) currentSpeed *= _sprintMultiplier;
            if (_isCrouching) currentSpeed *= _crouchMultiplier;
            
            return _moveDirection * currentSpeed;
        }

        private float GetNearestCardinalAngle(float angle)
        {
            float nearestAngle = _cardinalAngles[0];
            float smallestDifference = Mathf.Abs(Mathf.DeltaAngle(angle, nearestAngle));
            
            for (int i = 1; i < _cardinalAngles.Length; i++)
            {
                float difference = Mathf.Abs(Mathf.DeltaAngle(angle, _cardinalAngles[i]));
                if (difference < smallestDifference)
                {
                    smallestDifference = difference;
                    nearestAngle = _cardinalAngles[i];
                }
            }
            
            return nearestAngle;
        }

        private void HandleCrouchTransition()
        {
            if (_collider == null) return;
            
            float targetHeight = _isCrouching ? _crouchHeight : _standingHeight;
            float currentHeight = _collider.height;
            
            if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
            {
                float newHeight = Mathf.MoveTowards(currentHeight, targetHeight, _crouchTransitionSpeed * Time.deltaTime);
                _collider.height = newHeight;
                
                // Adjust center to keep feet on ground
                Vector3 center = _collider.center;
                center.y = newHeight * 0.5f;
                _collider.center = center;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set whether to use physics-based movement
        /// </summary>
        public void SetUsePhysics(bool usePhysics)
        {
            _usePhysics = usePhysics;
            
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = !_usePhysics;
            }
        }
        
        /// <summary>
        /// Set movement speed
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = Mathf.Max(0f, speed);
        }
        
        /// <summary>
        /// Enable or disable rotation snapping to cardinal directions
        /// </summary>
        public void SetSnapRotation(bool snap)
        {
            _snapRotation = snap;
        }
        #endregion

        #region Debug
        private void OnDrawGizmos()
        {
            if (!_showDebugInfo) return;
            
            // Draw ground check ray
            if (_groundCheckPoint != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawRay(_groundCheckPoint.position, Vector3.down * _groundCheckDistance);
            }
            
            // Draw movement direction
            if (_moveDirection.magnitude > 0.01f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position + Vector3.up, _moveDirection * 2f);
            }
            
            // Draw current velocity
            Vector3 velocity = _usePhysics && _rigidbody != null ? _rigidbody.linearVelocity : _currentVelocity;
            if (velocity.magnitude > 0.01f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, velocity);
            }
            
            // Draw cardinal direction indicators if snapping is enabled
            if (_snapRotation)
            {
                Gizmos.color = Color.yellow;
                foreach (float angle in _cardinalAngles)
                {
                    Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, direction * 0.5f);
                }
            }
        }
        #endregion
    }
}
