using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using UnityEngine.InputSystem;

namespace GameFramework.Components.Controllers.Movement
{
    /// <summary>
    /// Simple isometric movement component for top-down games.
    /// Handles basic movement with 4-directional rotation.
    /// Optional 45-degree offset for isometric camera alignment.
    /// </summary>
    ///
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class IsometricMovement : MonoBehaviour, IPlayerMovement
    {
        #region Serialized Fields
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5.0f;
        [SerializeField] private float _sprintMultiplier = 1.5f;
        [SerializeField] private float _crouchMultiplier = 0.5f;
        [SerializeField] private float _jumpForce = 5.0f;
        
        [Header("Rotation Settings")]
        [SerializeField] private bool _use45DegreeOffset = true; // Enable for isometric camera alignment
        
        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayerMask = 1;
        [SerializeField] private float _groundCheckDistance = 0.1f;
        
        [Header("Crouching")]
        [SerializeField] private float _crouchHeight = 1.0f;
        [SerializeField] private float _standingHeight = 2.0f;
        [SerializeField] private float _crouchTransitionSpeed = 5.0f;
        #endregion

        #region Private Fields
        private IPauseService _pauseService;
        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;

        
        // Movement state
        private Vector2 _moveInput = Vector2.zero;
        private bool _isInitialized = false;
        private Vector3 _currentVelocity = Vector3.zero;
        private bool _isJumpRequested = false;
        private bool _isSprinting = false;
        private bool _isCrouching = false;
        
        // Ground detection
        private bool _isGrounded = false;
        private Transform _groundCheckPoint;
        #endregion

        #region Public Properties
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        public Transform MovementTransform => transform;
        public Vector3 CurrentVelocity => _currentVelocity;
        public bool IsGrounded => _isGrounded;
        public bool Use45DegreeOffset => _use45DegreeOffset;
        public float MoveSpeed => _moveSpeed;
        
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();

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
            
            // Setup ground check point
            SetupGroundCheck();
            
            _isInitialized = true;
        }

        public void Cleanup()
        {
            _pauseService = null;
            _isInitialized = false;
            _currentVelocity = Vector3.zero;
            _moveInput = Vector2.zero;
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
            HandleMovement();
            HandleRotation();
            HandleCrouchTransition();
        }

        public void FixedUpdateMovement()
        {
            if (!_isInitialized || IsPaused) return;
            
            HandleJump();
        }

        public void StopMovement()
        {
            _moveInput = Vector2.zero;
            _currentVelocity = Vector3.zero;
            _isJumpRequested = false;
        }
        #endregion

        #region Private Methods
        private void HandleMovement()
        {
            if (_moveInput.magnitude < 0.01f)
            {
                _currentVelocity = Vector3.zero;
                return;
            }
            
            // Convert 2D input to 3D movement (isometric uses XZ plane)
            Vector3 movement = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;
            
            // Optionally rotate movement 45 degrees for isometric camera perspective
            if (_use45DegreeOffset)
            {
                movement = Quaternion.Euler(0f, 45f, 0f) * movement;
            }
            
            // Calculate movement speed with modifiers
            float currentSpeed = _moveSpeed;
            if (_isSprinting && !_isCrouching) currentSpeed *= _sprintMultiplier;
            if (_isCrouching) currentSpeed *= _crouchMultiplier;
            
            // Calculate velocity for this frame
            Vector3 velocity = movement * currentSpeed;
            _currentVelocity = velocity;
            
            // Apply movement
            transform.position += velocity * Time.deltaTime;
        }

        private void HandleRotation()
        {
            if (_moveInput.magnitude < 0.01f) return;
            
            // Calculate angle from input
            float angle = Mathf.Atan2(_moveInput.x, _moveInput.y) * Mathf.Rad2Deg;
            
            // Optionally add 45 degree offset to match isometric movement
            if (_use45DegreeOffset)
            {
                angle += 45f;
            }
            
            // Snap to nearest cardinal direction
            float targetAngle = GetNearestCardinalAngle(angle);
            
            // Set rotation directly to cardinal direction
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }

        private float GetNearestCardinalAngle(float angle)
        {
            // Get cardinal angles based on offset setting
            float[] cardinalAngles = _use45DegreeOffset 
                ? new float[] { 45f, 135f, 225f, 315f }    // NE, SE, SW, NW for isometric
                : new float[] { 0f, 90f, 180f, 270f };     // N, E, S, W for standard top-down
            
            // Normalize angle to 0-360 range
            while (angle < 0f) angle += 360f;
            while (angle >= 360f) angle -= 360f;
            
            float nearestAngle = cardinalAngles[0];
            float smallestDifference = Mathf.Abs(Mathf.DeltaAngle(angle, nearestAngle));
            
            for (int i = 1; i < cardinalAngles.Length; i++)
            {
                float difference = Mathf.Abs(Mathf.DeltaAngle(angle, cardinalAngles[i]));
                if (difference < smallestDifference)
                {
                    smallestDifference = difference;
                    nearestAngle = cardinalAngles[i];
                }
            }
            
            return nearestAngle;
        }
        
        private void SetupGroundCheck()
        {
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
            if (_groundCheckPoint == null) return;
            
            _isGrounded = Physics.Raycast(
                _groundCheckPoint.position,
                Vector3.down,
                _groundCheckDistance,
                _groundLayerMask
            );
        }

        private void HandleJump()
        {
            if (_isJumpRequested && _isGrounded && _rigidbody != null)
            {
                _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                _isJumpRequested = false;
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

        #region Public Methods
        /// <summary>
        /// Set movement speed
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = Mathf.Max(0f, speed);
        }

        #endregion
    }
}
