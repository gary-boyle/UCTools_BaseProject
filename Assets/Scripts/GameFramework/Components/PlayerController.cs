using UnityEngine;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;
using UnityEngine.InputSystem;

namespace GameFramework.Components
{
    /// <summary>
    /// Simple PlayerController that handles WASD movement on X and Z axes using Rigidbody physics.
    /// Integrates with the game's event-driven input system.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5.0f;
        [SerializeField] private float _sprintMultiplier = 1.5f;
        [SerializeField] private float _crouchMultiplier = 0.5f;
        [SerializeField] private float _jumpForce = 5.0f;
        
        [Header("Look Settings")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _mouseSensitivity = 2.0f;
        [SerializeField] private float _verticalLookRange = 80f;
        
        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayerMask = 1;
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private Transform _groundCheckPoint;
        
        [Header("Crouching")]
        [SerializeField] private float _crouchHeight = 1.0f;
        [SerializeField] private float _standingHeight = 2.0f;
        [SerializeField] private float _crouchTransitionSpeed = 5.0f;
        
        [Header("Attack Settings")]
        [SerializeField] private float _attackRange = 2.0f;
        [SerializeField] private LayerMask _attackLayerMask = -1;
        [SerializeField] private float _attackCooldown = 0.5f;
        
        [Header("Interaction Settings")]
        [SerializeField] private float _interactionRange = 3.0f;
        [SerializeField] private LayerMask _interactionLayerMask = -1;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private IEventSystem _eventSystem;
        private Rigidbody _rigidbody;
        private CapsuleCollider _capsuleCollider;
        
        // Movement state
        private Vector2 _inputVector = Vector2.zero;
        private Vector3 _targetVelocity = Vector3.zero;
        private bool _isGrounded = true;
        private bool _isSprinting = false;
        private bool _isCrouching = false;
        private bool _wantsToJump = false;
        
        // Look state
        private Vector2 _lookInput = Vector2.zero;
        private float _verticalRotation = 0f;
        
        // Action states
        private float _lastAttackTime = 0f;
        private int _currentItemIndex = 0;
        
        // Component state
        private bool _isInitialized = false;
        
        // Original height for crouching
        private float _originalHeight;
        #endregion

        #region Public Properties
        public float MoveSpeed 
        { 
            get => _moveSpeed; 
            set => _moveSpeed = Mathf.Max(0f, value); 
        }
        
        public Vector3 CurrentVelocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;
        public Vector2 InputVector => _inputVector;
        public Vector2 LookInput => _lookInput;
        public bool IsMoving => _inputVector.magnitude > 0.01f;
        public bool IsGrounded => _isGrounded;
        public bool IsSprinting => _isSprinting;
        public bool IsCrouching => _isCrouching;
        public int CurrentItemIndex => _currentItemIndex;
        
        /// <summary>
        /// Get the effective move speed based on current modifiers
        /// </summary>
        public float EffectiveMoveSpeed
        {
            get
            {
                float speed = _moveSpeed;
                if (_isSprinting && !_isCrouching) speed *= _sprintMultiplier;
                if (_isCrouching) speed *= _crouchMultiplier;
                return speed;
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogError($"[PlayerController] Rigidbody component not found on {gameObject.name}");
                return;
            }
            
            _capsuleCollider = GetComponent<CapsuleCollider>();
            if (_capsuleCollider == null)
            {
                Debug.LogError($"[PlayerController] CapsuleCollider component not found on {gameObject.name}");
                return;
            }
            
            // Configure rigidbody for character movement
            _rigidbody.freezeRotation = true; // Prevent physics rotation
            
            // Store original height for crouching
            _originalHeight = _capsuleCollider.height;
            
            // Set up camera transform if not assigned
            if (_cameraTransform == null)
            {
                _cameraTransform = Camera.main?.transform;
                if (_cameraTransform == null)
                {
                    // Try to find camera as child
                    _cameraTransform = GetComponentInChildren<Camera>()?.transform;
                }
            }
            
            // Create ground check point if not assigned
            if (_groundCheckPoint == null)
            {
                GameObject groundCheck = new GameObject("GroundCheckPoint");
                groundCheck.transform.SetParent(transform);
                groundCheck.transform.localPosition = new Vector3(0, 0, 0);
                _groundCheckPoint = groundCheck.transform;
            }
        }

        private void Start()
        {
            InitializeController();
        }

        private void Update()
        {
            if (!_isInitialized) return;
            
            CheckGrounded();
            ProcessMovement();
            ProcessLook();
            ProcessCrouching();
        }

        private void FixedUpdate()
        {
            if (!_isInitialized) return;
            
            // Physics-based movement should happen in FixedUpdate
            ApplyMovement();
            ProcessJump();
        }

        private void OnDestroy()
        {
            CleanupController();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the PlayerController and subscribe to input events
        /// </summary>
        private void InitializeController()
        {
            // Get EventSystem from GameContext
            _eventSystem = GameManager.GetService<IEventSystem>();
            if (_eventSystem == null)
            {
                Debug.LogError($"[PlayerController] EventSystem not available.");
                return;
            }

            // Subscribe to all player input events
            _eventSystem.Subscribe<PlayerMoveInputEvent>(OnPlayerMoveInput);
            _eventSystem.Subscribe<PlayerLookInputEvent>(OnPlayerLookInput);
            _eventSystem.Subscribe<PlayerJumpInputEvent>(OnPlayerJumpInput);
            _eventSystem.Subscribe<PlayerSprintInputEvent>(OnPlayerSprintInput);
            _eventSystem.Subscribe<PlayerCrouchInputEvent>(OnPlayerCrouchInput);
            _eventSystem.Subscribe<PlayerAttackInputEvent>(OnPlayerAttackInput);
            _eventSystem.Subscribe<PlayerInteractInputEvent>(OnPlayerInteractInput);
            _eventSystem.Subscribe<PlayerPreviousInputEvent>(OnPlayerPreviousInput);
            _eventSystem.Subscribe<PlayerNextInputEvent>(OnPlayerNextInput);
            
            _isInitialized = true;
            
            if (_showDebugInfo)
                Debug.Log($"[PlayerController] Initialized successfully on {gameObject.name}");
        }

        /// <summary>
        /// Cleanup subscriptions when the controller is destroyed
        /// </summary>
        private void CleanupController()
        {
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<PlayerMoveInputEvent>(OnPlayerMoveInput);
                _eventSystem.Unsubscribe<PlayerLookInputEvent>(OnPlayerLookInput);
                _eventSystem.Unsubscribe<PlayerJumpInputEvent>(OnPlayerJumpInput);
                _eventSystem.Unsubscribe<PlayerSprintInputEvent>(OnPlayerSprintInput);
                _eventSystem.Unsubscribe<PlayerCrouchInputEvent>(OnPlayerCrouchInput);
                _eventSystem.Unsubscribe<PlayerAttackInputEvent>(OnPlayerAttackInput);
                _eventSystem.Unsubscribe<PlayerInteractInputEvent>(OnPlayerInteractInput);
                _eventSystem.Unsubscribe<PlayerPreviousInputEvent>(OnPlayerPreviousInput);
                _eventSystem.Unsubscribe<PlayerNextInputEvent>(OnPlayerNextInput);
            }
            
            if (_showDebugInfo)
                Debug.Log($"[PlayerController] Cleaned up on {gameObject.name}");
        }
        #endregion

        #region Input Event Handling
        /// <summary>
        /// Handle player movement input events from the input system
        /// </summary>
        private void OnPlayerMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized) return;

            // Handle all phases and check for zero vector
            if (inputEvent.Phase == InputActionPhase.Performed || 
                inputEvent.Phase == InputActionPhase.Started)
            {
                // Set input vector, even if it's zero (this handles key release)
                _inputVector = inputEvent.MovementVector;
            }
            else if (inputEvent.Phase == InputActionPhase.Canceled)
            {
                _inputVector = Vector2.zero;
            }

            // Additional safety: if the movement vector is very small, treat as no input
            if (_inputVector.magnitude < 0.01f)
            {
                _inputVector = Vector2.zero;
            }

            if (_showDebugInfo)
            {
                Debug.Log($"[PlayerController] Movement Input: {_inputVector}, Phase: {inputEvent.Phase}");
            }
        }
        
        /// <summary>
        /// Handle player look input events for camera/mouse look
        /// </summary>
        private void OnPlayerLookInput(PlayerLookInputEvent inputEvent)
        {
            if (!_isInitialized) return;

            _lookInput = inputEvent.LookDelta;

            if (_showDebugInfo)
            {
                Debug.Log($"[PlayerController] Look Input: {_lookInput}, Phase: {inputEvent.Phase}");
            }
        }
        
        /// <summary>
        /// Handle player jump input events
        /// </summary>
        private void OnPlayerJumpInput(PlayerJumpInputEvent inputEvent)
        {
            if (!_isInitialized) return;

            if (_isGrounded && !_wantsToJump)
            {
                _wantsToJump = true;
                
                if (_showDebugInfo)
                {
                    Debug.Log("[PlayerController] Jump Input Received");
                }
            }
        }
        
        /// <summary>
        /// Handle player sprint input events
        /// </summary>
        private void OnPlayerSprintInput(PlayerSprintInputEvent inputEvent)
        {
            if (!_isInitialized) return;

            switch (inputEvent.Phase)
            {
                case InputActionPhase.Started:
                case InputActionPhase.Performed:
                    _isSprinting = true;
                    break;
                case InputActionPhase.Canceled:
                    _isSprinting = false;
                    break;
            }

            if (_showDebugInfo)
            {
                Debug.Log($"[PlayerController] Sprint: {_isSprinting}, Phase: {inputEvent.Phase}");
            }
        }
        
        /// <summary>
        /// Handle player crouch input events
        /// </summary>
        private void OnPlayerCrouchInput(PlayerCrouchInputEvent inputEvent)
        {
            if (!_isInitialized) return;

            switch (inputEvent.Phase)
            {
                case InputActionPhase.Started:
                case InputActionPhase.Performed:
                    _isCrouching = !_isCrouching; // Toggle crouch
                    break;
            }

            if (_showDebugInfo)
            {
                Debug.Log($"[PlayerController] Crouch: {_isCrouching}, Phase: {inputEvent.Phase}");
            }
        }
        
        /// <summary>
        /// Handle player attack input events
        /// </summary>
        private void OnPlayerAttackInput(PlayerAttackInputEvent inputEvent)
        {
            if (!_isInitialized) return;

            if (inputEvent.Phase == InputActionPhase.Performed)
            {
                PerformAttack();
            }
        }
        
        /// <summary>
        /// Handle player interact input events
        /// </summary>
        private void OnPlayerInteractInput(PlayerInteractInputEvent inputEvent)
        {
            if (!_isInitialized) return;

            if (inputEvent.Phase == InputActionPhase.Performed)
            {
                PerformInteraction();
            }
        }
        
        /// <summary>
        /// Handle player previous item input events
        /// </summary>
        private void OnPlayerPreviousInput(PlayerPreviousInputEvent inputEvent)
        {
            if (!_isInitialized) return;

            _currentItemIndex = Mathf.Max(0, _currentItemIndex - 1);
            
            if (_showDebugInfo)
            {
                Debug.Log($"[PlayerController] Previous Item: Index {_currentItemIndex}");
            }
        }
        
        /// <summary>
        /// Handle player next item input events
        /// </summary>
        private void OnPlayerNextInput(PlayerNextInputEvent inputEvent)
        {
            if (!_isInitialized) return;

            _currentItemIndex++; // Could add max limit if needed
            
            if (_showDebugInfo)
            {
                Debug.Log($"[PlayerController] Next Item: Index {_currentItemIndex}");
            }
        }
        #endregion

        #region Movement Processing
        /// <summary>
        /// Check if the player is grounded using raycast
        /// </summary>
        private void CheckGrounded()
        {
            if (_groundCheckPoint == null) return;
            
            _isGrounded = Physics.Raycast(_groundCheckPoint.position, Vector3.down, _groundCheckDistance, _groundLayerMask);
            
            if (_showDebugInfo)
            {
                Color rayColor = _isGrounded ? Color.green : Color.red;
                Debug.DrawRay(_groundCheckPoint.position, Vector3.down * _groundCheckDistance, rayColor);
            }
        }

        /// <summary>
        /// Process movement input and calculate target velocity
        /// </summary>
        private void ProcessMovement()
        {
            // Convert 2D input to 3D movement (X and Z axes only)
            Vector3 inputDirection = new Vector3(_inputVector.x, 0f, _inputVector.y);
            
            // Transform input direction relative to player facing direction
            Vector3 worldDirection = transform.TransformDirection(inputDirection);
            worldDirection.y = 0f; // Keep movement horizontal
            
            // Calculate target velocity based on input using effective move speed
            _targetVelocity = worldDirection * EffectiveMoveSpeed;
            
            // Debug info
            if (_showDebugInfo)
            {
                Debug.Log($"[PlayerController] Input: {_inputVector}, Target Velocity: {_targetVelocity}, Speed: {EffectiveMoveSpeed}, Sprint: {_isSprinting}, Crouch: {_isCrouching}");
                
                if (IsMoving)
                {
                    Debug.DrawRay(transform.position, _targetVelocity.normalized * 2f, Color.green);
                }
            }
        }
        
        /// <summary>
        /// Process look input for camera rotation
        /// </summary>
        private void ProcessLook()
        {
            if (_cameraTransform == null || _lookInput.magnitude < 0.01f) return;

            // Apply mouse sensitivity (note: InputManager already applies settings)
            Vector2 lookDelta = _lookInput * Time.deltaTime;

            // Rotate player horizontally
            transform.Rotate(Vector3.up, lookDelta.x, Space.World);

            // Rotate camera vertically
            _verticalRotation -= lookDelta.y;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -_verticalLookRange, _verticalLookRange);
            _cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
        }
        
        /// <summary>
        /// Process jumping in FixedUpdate
        /// </summary>
        private void ProcessJump()
        {
            if (_wantsToJump && _isGrounded && _rigidbody != null)
            {
                // Apply jump force
                _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                _wantsToJump = false;
                
                if (_showDebugInfo)
                {
                    Debug.Log($"[PlayerController] Jump executed with force {_jumpForce}");
                }
            }
        }
        
        /// <summary>
        /// Process crouching height changes
        /// </summary>
        private void ProcessCrouching()
        {
            if (_capsuleCollider == null) return;

            float targetHeight = _isCrouching ? _crouchHeight : _standingHeight;
            float currentHeight = _capsuleCollider.height;
            
            if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
            {
                float newHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * _crouchTransitionSpeed);
                _capsuleCollider.height = newHeight;
                
                // Adjust center to keep feet on ground
                _capsuleCollider.center = new Vector3(
                    _capsuleCollider.center.x, 
                    newHeight * 0.5f, 
                    _capsuleCollider.center.z);
            }
        }

        /// <summary>
        /// Apply movement to the rigidbody - immediate response to input
        /// </summary>
        private void ApplyMovement()
        {
            if (_rigidbody == null) return;

            // Get current velocity and preserve Y component (gravity/vertical movement)
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            
            // Set horizontal velocity directly based on input (immediate response)
            Vector3 newVelocity = new Vector3(
                _targetVelocity.x,
                currentVelocity.y, // Preserve Y velocity (gravity)
                _targetVelocity.z
            );
            
            _rigidbody.linearVelocity = newVelocity;
            
            // Debug info
            if (_showDebugInfo)
            {
                Debug.Log($"[PlayerController] Applied Velocity: {newVelocity} (was: {currentVelocity})");
                Debug.DrawRay(transform.position, _rigidbody.linearVelocity, Color.blue);
            }
        }
        
        /// <summary>
        /// Perform an attack action
        /// </summary>
        private void PerformAttack()
        {
            if (Time.time < _lastAttackTime + _attackCooldown) return;
            
            _lastAttackTime = Time.time;
            
            // Raycast forward to detect attackable objects
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _attackRange, _attackLayerMask))
            {
                if (_showDebugInfo)
                {
                    Debug.Log($"[PlayerController] Attack hit: {hit.collider.name}");
                }
                
                // Could publish an attack event or call methods on hit objects here
                // Example: hit.collider.GetComponent<IDamageable>()?.TakeDamage(attackDamage);
            }
            else if (_showDebugInfo)
            {
                Debug.Log("[PlayerController] Attack missed");
            }
        }
        
        /// <summary>
        /// Perform an interaction action
        /// </summary>
        private void PerformInteraction()
        {
            // Raycast forward to detect interactable objects
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _interactionRange, _interactionLayerMask))
            {
                if (_showDebugInfo)
                {
                    Debug.Log($"[PlayerController] Interaction with: {hit.collider.name}");
                }
                
                // Could publish an interaction event or call methods on hit objects here
                // Example: hit.collider.GetComponent<IInteractable>()?.Interact(this);
            }
            else if (_showDebugInfo)
            {
                Debug.Log("[PlayerController] No interactable object found");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually set movement input (useful for testing or external control)
        /// </summary>
        public void SetMovementInput(Vector2 input)
        {
            _inputVector = input;
        }

        /// <summary>
        /// Stop all movement immediately
        /// </summary>
        public void StopMovement()
        {
            _inputVector = Vector2.zero;
            _targetVelocity = Vector3.zero;
            
            if (_rigidbody != null)
            {
                // Stop horizontal movement, preserve vertical velocity for gravity
                Vector3 velocity = _rigidbody.linearVelocity;
                _rigidbody.linearVelocity = new Vector3(0f, velocity.y, 0f);
            }
        }

        /// <summary>
        /// Get current movement direction in world space
        /// </summary>
        public Vector3 GetMovementDirection()
        {
            if (_rigidbody == null) return Vector3.zero;
            
            // Return horizontal movement direction only
            Vector3 horizontalVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            return horizontalVelocity.normalized;
        }
        
        /// <summary>
        /// Manually set look input (useful for testing or external control)
        /// </summary>
        public void SetLookInput(Vector2 lookInput)
        {
            _lookInput = lookInput;
        }
        
        /// <summary>
        /// Manually trigger jump
        /// </summary>
        public void Jump()
        {
            if (_isGrounded && !_wantsToJump)
            {
                _wantsToJump = true;
            }
        }
        
        /// <summary>
        /// Set sprint state
        /// </summary>
        public void SetSprinting(bool sprinting)
        {
            _isSprinting = sprinting;
        }
        
        /// <summary>
        /// Set crouch state
        /// </summary>
        public void SetCrouching(bool crouching)
        {
            _isCrouching = crouching;
        }
        
        /// <summary>
        /// Set current item index
        /// </summary>
        public void SetItemIndex(int index)
        {
            _currentItemIndex = Mathf.Max(0, index);
        }
        
        /// <summary>
        /// Trigger attack manually
        /// </summary>
        public void Attack()
        {
            PerformAttack();
        }
        
        /// <summary>
        /// Trigger interaction manually
        /// </summary>
        public void Interact()
        {
            PerformInteraction();
        }
        #endregion

        #region Debug
        private void OnDrawGizmos()
        {
            if (!_showDebugInfo) return;

            if (Application.isPlaying && _rigidbody != null)
            {
                // Draw current velocity vector
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + _rigidbody.linearVelocity);
                
                // Draw target velocity vector
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, transform.position + _targetVelocity);
            }
            
            // Draw input direction
            Gizmos.color = Color.red;
            Vector3 inputDir = new Vector3(_inputVector.x, 0f, _inputVector.y);
            inputDir = transform.TransformDirection(inputDir);
            Gizmos.DrawLine(transform.position, transform.position + inputDir * 2f);
            
            // Draw ground check
            if (_groundCheckPoint != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawLine(_groundCheckPoint.position, _groundCheckPoint.position + Vector3.down * _groundCheckDistance);
                Gizmos.DrawWireSphere(_groundCheckPoint.position + Vector3.down * _groundCheckDistance, 0.1f);
            }
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * _attackRange, 0.2f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * _attackRange);
            
            // Draw interaction range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + transform.forward * _interactionRange, 0.15f);
            
            // Draw look direction if camera exists
            if (_cameraTransform != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(_cameraTransform.position, _cameraTransform.forward * 3f);
            }
            
            // Show state indicators
            Vector3 statusPos = transform.position + Vector3.up * 2.5f;
            if (_isSprinting)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(statusPos, Vector3.one * 0.2f);
            }
            if (_isCrouching)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(statusPos + Vector3.right * 0.5f, Vector3.one * 0.2f);
            }
        }
        #endregion
    }
}
