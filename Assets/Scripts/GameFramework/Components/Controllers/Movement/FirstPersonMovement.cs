using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using UnityEngine.InputSystem;

namespace GameFramework.Components.Controllers.Movement
{
    /// <summary>
    /// First-person movement component that handles WASD movement, jumping, sprinting, and crouching.
    /// Uses Rigidbody physics for realistic movement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class FirstPersonMovement : MonoBehaviour, IPlayerMovement
    {
        #region Serialized Fields
        
        [Header("GameObject References")]
        [SerializeField] private Transform _groundCheckPoint;
        
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5.0f;
        [SerializeField] private float _sprintMultiplier = 1.5f;
        [SerializeField] private float _crouchMultiplier = 0.5f;
        [SerializeField] private float _jumpForce = 5.0f;
        [SerializeField] private float _airControl = 0.5f;
        
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
        private bool _isJumpRequested = false;
        private bool _isSprinting = false;
        private bool _isCrouching = false;
        
        // Ground detection
        private bool _isGrounded = false;
        
        // Component state
        private bool _isInitialized = false;
        #endregion

        #region Public Properties
        
        public bool IsPaused => _pauseService?.IsPaused ?? false;
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

            if (_groundCheckPoint == null)
            {
                Debug.LogError("[FirstPersonMovement] GroundCheckPoint is required but not assigned.");
                return;
            }

            // Configure rigidbody
            _rigidbody.freezeRotation = true;
            
            _isInitialized = true;
        }

        public void Cleanup()
        {
            _pauseService = null;
            _isInitialized = false;
        }

        public void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            _moveInput = inputEvent.MovementVector;
        }

        public void HandleJumpInput(PlayerJumpInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            if (inputEvent.Phase == InputActionPhase.Performed && _isGrounded)
            {
                _isJumpRequested = true;
            }
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
        }

        public void FixedUpdateMovement()
        {
            if (!_isInitialized || IsPaused) return;
            
            HandleMovement();
            HandleJump();
        }

        public void StopMovement()
        {
            _moveInput = Vector2.zero;
            _moveDirection = Vector3.zero;
            _isJumpRequested = false;
            
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = new Vector3(0, _rigidbody.linearVelocity.y, 0);
            }
        }
        #endregion

        #region Private Methods

        private void CheckGrounded()
        {
            if (_groundCheckPoint == null) return;
            
            _isGrounded = Physics.Raycast(
                _groundCheckPoint.position,
                Vector3.down,
                _groundCheckDistance,
                _groundLayerMask
            );
        }

        private void HandleMovement()
        {
            if (_moveInput.magnitude < 0.01f)
            {
                // Apply ground friction when not moving
                if (_isGrounded)
                {
                    Vector3 velocity = _rigidbody.linearVelocity;
                    velocity.x = Mathf.MoveTowards(velocity.x, 0, _moveSpeed * Time.fixedDeltaTime);
                    velocity.z = Mathf.MoveTowards(velocity.z, 0, _moveSpeed * Time.fixedDeltaTime);
                    _rigidbody.linearVelocity = velocity;
                }
                return;
            }

            // Convert input to world space movement
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            _moveDirection = (forward * _moveInput.y + right * _moveInput.x).normalized;
            
            // Calculate movement speed with modifiers
            float currentSpeed = _moveSpeed;
            if (_isSprinting && !_isCrouching) currentSpeed *= _sprintMultiplier;
            if (_isCrouching) currentSpeed *= _crouchMultiplier;
            
            // Apply movement force
            Vector3 targetVelocity = _moveDirection * currentSpeed;
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            
            // Different handling for grounded vs airborne movement
            if (_isGrounded)
            {
                // Direct velocity assignment for responsive ground movement
                _rigidbody.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
            }
            else
            {
                // Reduced air control
                Vector3 velocityChange = (targetVelocity - new Vector3(currentVelocity.x, 0, currentVelocity.z)) * _airControl;
                _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }

        private void HandleJump()
        {
            if (_isJumpRequested && _isGrounded)
            {
                _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                _isJumpRequested = false;
                
                if (_showDebugInfo)
                    Debug.Log("[FirstPersonMovement] Jump executed");
            }
            
            _isJumpRequested = false;
        }

        private void HandleCrouchTransition()
        {
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

        #region Debug
        private void OnDrawGizmos()
        {
            if (!_showDebugInfo || _groundCheckPoint == null) return;
            
            // Draw ground check ray
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(_groundCheckPoint.position, Vector3.down * _groundCheckDistance);
            
            // Draw movement direction
            if (_moveDirection.magnitude > 0.01f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, _moveDirection * 2f);
            }
        }
        #endregion
    }
}
