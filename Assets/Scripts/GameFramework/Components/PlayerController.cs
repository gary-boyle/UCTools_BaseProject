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
        
        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayerMask = 1;
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private Transform _groundCheckPoint;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private IEventSystem _eventSystem;
        private Rigidbody _rigidbody;
        
        // Movement state
        private Vector2 _inputVector = Vector2.zero;
        private Vector3 _targetVelocity = Vector3.zero;
        private bool _isGrounded = true;
        
        // Component state
        private bool _isInitialized = false;
        #endregion

        #region Public Properties
        public float MoveSpeed 
        { 
            get => _moveSpeed; 
            set => _moveSpeed = Mathf.Max(0f, value); 
        }
        
        public Vector3 CurrentVelocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;
        public Vector2 InputVector => _inputVector;
        public bool IsMoving => _inputVector.magnitude > 0.01f;
        public bool IsGrounded => _isGrounded;
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
            
            // Configure rigidbody for character movement
            _rigidbody.freezeRotation = true; // Prevent physics rotation
            
            // Create ground check point if not assigned
            // if (_groundCheckPoint == null)
            // {
            //     GameObject groundCheck = new GameObject("GroundCheckPoint");
            //     groundCheck.transform.SetParent(transform);
            //     groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);
            //     _groundCheckPoint = groundCheck.transform;
            // }
        }

        private void Start()
        {
            InitializeController();
        }

        private void Update()
        {
            if (!_isInitialized) return;
            
            //CheckGrounded();
            ProcessMovement();
        }

        private void FixedUpdate()
        {
            if (!_isInitialized) return;
            
            // Physics-based movement should happen in FixedUpdate
            ApplyMovement();
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

            // Subscribe to player movement input events
            _eventSystem.Subscribe<PlayerMoveInputEvent>(OnPlayerMoveInput);
            
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
            
            // Calculate target velocity based on input (only horizontal movement)
            _targetVelocity = inputDirection * _moveSpeed;
            
            // Debug info
            if (_showDebugInfo)
            {
                Debug.Log($"[PlayerController] Input: {_inputVector}, Target Velocity: {_targetVelocity}, IsMoving: {IsMoving}");
                
                if (IsMoving)
                {
                    Debug.DrawRay(transform.position, _targetVelocity.normalized * 2f, Color.green);
                }
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
            Gizmos.DrawLine(transform.position, transform.position + inputDir * 2f);
            
            // Draw ground check
            if (_groundCheckPoint != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawLine(_groundCheckPoint.position, _groundCheckPoint.position + Vector3.down * _groundCheckDistance);
                Gizmos.DrawWireSphere(_groundCheckPoint.position + Vector3.down * _groundCheckDistance, 0.1f);
            }
        }
        #endregion
    }
}
