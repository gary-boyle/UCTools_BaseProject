using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.Components.Controllers.Enum;
using UnityEngine.InputSystem;

namespace GameFramework.Components.Controllers.Movement
{
    /// <summary>
    /// Third-person movement component that handles character movement relative to camera direction.
    /// Includes smooth rotation towards movement direction and camera-relative input.
    /// </summary>

    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class ThirdPersonMovement : MonoBehaviour, IPlayerMovement
    {
        #region Serialized Fields
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5.0f;
        [SerializeField] private float _sprintMultiplier = 1.5f;
        [SerializeField] private float _crouchMultiplier = 0.5f;
        [SerializeField] private float _jumpForce = 5.0f;
        [SerializeField] private float _airControl = 0.3f;
        
        [Header("Rotation Settings")]
        [SerializeField] private CharacterRotationSettings _rotationSettings = new CharacterRotationSettings();
        
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
        private UnityEngine.Camera _mainCamera;
        
        // Movement state
        private Vector2 _moveInput = Vector2.zero;
        private Vector3 _moveDirection = Vector3.zero;
        private bool _isJumpRequested = false;
        private bool _isSprinting = false;
        private bool _isCrouching = false;
        
        // Rotation state - simplified
        
        // Input state
        private Vector2 _lookInput = Vector2.zero;
        private float _currentYaw = 0f;
        private float _timeSinceLastMouseInput = 0f;
        
        // Hybrid mode state
        private bool _isUsingMouseRotation = false;
        
        // Ground detection
        private bool _isGrounded = false;
        private Transform _groundCheckPoint;
        
        // Component state
        private bool _isInitialized = false;
        
        #endregion

        #region Public Properties
        
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        public Transform MovementTransform => transform;
        public Vector3 CurrentVelocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;
        public bool IsGrounded => _isGrounded;
        public bool IsCrouching => _isCrouching;
        public bool IsJumping => !_isGrounded && _rigidbody != null && _rigidbody.linearVelocity.y > 0.1f;
        public float CurrentYaw => _currentYaw;
        public CharacterRotationMode RotationMode => _rotationSettings.rotationMode;
        public bool IsUsingMouseRotation => _isUsingMouseRotation;
        
        public float MoveSpeed => _moveSpeed;
        
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get required components
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();
            _mainCamera = UnityEngine.Camera.main;
            
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
            if (_mainCamera == null)
            {
                Debug.LogWarning($"[ThirdPersonMovement] No main camera found. Movement will be relative to world space.");
            }

            // Setup ground check point
            SetupGroundCheck();
            
            // Configure rigidbody
            _rigidbody.freezeRotation = true;
            
            // Initialize current yaw from transform
            _currentYaw = transform.eulerAngles.y;
            
            _isInitialized = true;
            
            if (_showDebugInfo)
            {
                Debug.Log($"[ThirdPersonMovement] Initialized with rotation mode: {_rotationSettings.rotationMode}");
                LogRotationSettings();
            }
        }

        public void Cleanup()
        {
            _pauseService = null;
            _mainCamera = null;
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
            
            if (inputEvent.Phase == InputActionPhase.Performed)
                _isSprinting = true;
            else if (inputEvent.Phase == InputActionPhase.Canceled)
                _isSprinting = false;
        }

        public void HandleCrouchInput(PlayerCrouchInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            if (inputEvent.Phase == InputActionPhase.Performed)
                _isCrouching = true;
            else if (inputEvent.Phase == InputActionPhase.Canceled)
                _isCrouching = false;
        }
        
        public void HandleLookInput(PlayerLookInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            _lookInput = inputEvent.LookDelta;
            
            // Reset mouse inactivity timer if there's significant input
            if (_lookInput.magnitude > 0.01f)
            {
                _timeSinceLastMouseInput = 0f;
                _isUsingMouseRotation = true;
            }
        }

        public void UpdateMovement()
        {
            if (!_isInitialized || IsPaused) return;
            Debug.Log("crouching" + _isCrouching);
            CheckGrounded();
            HandleCrouchTransition();
            UpdateRotation();
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
        
        /// <summary>
        /// Changes the character's rotation mode at runtime
        /// </summary>
        public void SetRotationMode(CharacterRotationMode mode)
        {
            _rotationSettings.rotationMode = mode;
            
            // Reset state when switching modes
            _timeSinceLastMouseInput = 0f;
            _isUsingMouseRotation = false;
            
            if (_showDebugInfo)
            {
                Debug.Log($"[ThirdPersonMovement] Rotation mode changed to: {mode}");
                LogRotationSettings();
            }
        }
        
        /// <summary>
        /// Gets the current rotation settings (read-only access)
        /// </summary>
        public CharacterRotationSettings GetRotationSettings()
        {
            return _rotationSettings;
        }
        
        private void LogRotationSettings()
        {
            if (!_showDebugInfo) return;
            
            Debug.Log($"[ThirdPersonMovement] Rotation Settings - Mode: {_rotationSettings.rotationMode}");
            Debug.Log($"  Movement Rotation Speed: {_rotationSettings.movementRotationSpeed} " +
                     $"(Direct Input: {_rotationSettings.movementRotationSpeed * 100f}°/s)");
            Debug.Log($"  Mouse Sensitivity: {_rotationSettings.mouseRotationSensitivity}");
            if (_rotationSettings.rotationMode == CharacterRotationMode.MouseWithMovementFallback)
            {
                Debug.Log($"  Hybrid Inactivity Threshold: {_rotationSettings.mouseInactivityThreshold}s");
            }
        }
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Determines if A/D input should be used for rotation instead of movement
        /// </summary>
        private bool ShouldUseADForRotation()
        {
            bool hasHorizontalInput = Mathf.Abs(_moveInput.x) > 0.01f;
            bool hasForwardInput = Mathf.Abs(_moveInput.y) > 0.01f;
            
            // A/D is only used for rotation when pressed alone (not with W/S)
            bool horizontalOnly = hasHorizontalInput && !hasForwardInput;
            if (!horizontalOnly) return false;
            
            switch (_rotationSettings.rotationMode)
            {
                case CharacterRotationMode.TankControls:
                    return true; // Always use A/D for rotation in tank mode
                    
                case CharacterRotationMode.MouseWithMovementFallback:
                    // Only use A/D for rotation when mouse is inactive
                    return _timeSinceLastMouseInput >= _rotationSettings.mouseInactivityThreshold;
                    
                default:
                    return false; // Other modes don't use A/D for rotation
            }
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

        private void HandleMovement()
        {
            // Determine effective movement input based on rotation mode
            Vector2 effectiveInput = _moveInput;
            
            // Check if A/D input should be used for rotation instead of movement
            if (ShouldUseADForRotation())
            {
                // A/D is being used for rotation, don't use it for movement
                effectiveInput.x = 0f;
                
                if (_showDebugInfo)
                {
                    Debug.Log($"[ThirdPersonMovement] A/D input intercepted for rotation (mode: {_rotationSettings.rotationMode})");
                }
            }
            
            if (effectiveInput.magnitude < 0.01f)
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

            // Calculate movement direction relative to character's facing direction
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            // W/S = forward/backward, A/D = strafe left/right (when not used for rotation)
            _moveDirection = (forward * effectiveInput.y + right * effectiveInput.x).normalized;
            
            if (_showDebugInfo && effectiveInput.magnitude > 0.5f)
            {
                Debug.Log($"[ThirdPersonMovement] Movement - Input: {effectiveInput}, Direction: {_moveDirection}");
            }
            
            // Calculate movement speed with modifiers
            float currentSpeed = _moveSpeed;
            if (_isSprinting && !_isCrouching) currentSpeed *= _sprintMultiplier;
            if (_isCrouching) currentSpeed *= _crouchMultiplier;
            
            // Apply movement
            Vector3 targetVelocity = _moveDirection * currentSpeed;
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            
            if (_isGrounded)
            {
                // Direct movement on ground
                _rigidbody.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
            }
            else
            {
                // Reduced air control
                Vector3 velocityChange = (targetVelocity - new Vector3(currentVelocity.x, 0, currentVelocity.z)) * _airControl;
                _rigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }

        private void UpdateRotation()
        {
            // Update mouse inactivity timer
            _timeSinceLastMouseInput += Time.deltaTime;
            
            switch (_rotationSettings.rotationMode)
            {
                case CharacterRotationMode.None:
                    // No rotation updates
                    break;
                    
                case CharacterRotationMode.TankControls:
                    HandleMovementDirectionRotation();
                    break;
                    
                case CharacterRotationMode.MouseControl:
                    HandleMouseRotation();
                    break;
                    
                case CharacterRotationMode.MouseWithMovementFallback:
                    HandleHybridRotation();
                    break;
            }
            
            // Reset look input after processing
            _lookInput = Vector2.zero;
        }
        
        private void HandleMouseRotation()
        {
            if (_lookInput.magnitude < 0.01f) return;
            
            // Process horizontal mouse input for character rotation
            float horizontalInput = _lookInput.x * _rotationSettings.mouseRotationSensitivity;
            
            // Apply mouse rotation directly
            _currentYaw += horizontalInput;
            transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        }
        
        private void HandleMovementDirectionRotation()
        {
            // Use the consistent helper method
            if (ShouldUseADForRotation())
            {
                // Direct rotational input - rotate the character with A/D keys
                float rotationInput = _moveInput.x * _rotationSettings.movementRotationSpeed * 100f;
                _currentYaw += rotationInput * Time.deltaTime;
                transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
                
                if (_showDebugInfo)
                {
                    Debug.Log($"[ThirdPersonMovement] Tank rotation: {rotationInput:F1}°/s (A/D keys)");
                }
            }
        }
        
        private void HandleHybridRotation()
        {
            bool hasMouseInput = _lookInput.magnitude > 0.01f;
            bool shouldUseMouseRotation = hasMouseInput || _timeSinceLastMouseInput < _rotationSettings.mouseInactivityThreshold;
            
            if (shouldUseMouseRotation)
            {
                // Use mouse rotation when mouse is active or recently used
                if (hasMouseInput)
                {
                    HandleMouseRotation();
                }
            }
            else if (ShouldUseADForRotation())
            {
                // Mouse is inactive and A/D should be used for rotation
                float rotationInput = _moveInput.x * _rotationSettings.movementRotationSpeed * 100f;
                _currentYaw += rotationInput * Time.deltaTime;
                transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
                
                if (_showDebugInfo)
                {
                    Debug.Log($"[ThirdPersonMovement] Hybrid A/D rotation: {rotationInput:F1}°/s (mouse inactive)");
                }
            }
            // When moving diagonally (W+A, W+D), A/D is used for movement, not rotation
        }

        private void HandleJump()
        {
            if (_isJumpRequested && _isGrounded)
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
            
            // Draw rotation mode information
            DrawRotationModeGizmos();
        }
        
        private void DrawRotationModeGizmos()
        {
            Vector3 basePosition = transform.position + Vector3.up * 0.5f;
            
            switch (_rotationSettings.rotationMode)
            {
                case CharacterRotationMode.None:
                    // Draw a gray circle to indicate no rotation
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireSphere(basePosition, 0.3f);
                    break;
                    
                case CharacterRotationMode.TankControls:
                    // Check if we're using direct rotation input
                    bool isUsingDirectRotation = Mathf.Abs(_moveInput.x) > 0.01f && Mathf.Abs(_moveInput.y) < 0.01f;
                    
                    if (isUsingDirectRotation)
                    {
                        // Draw current facing direction in cyan for direct rotation
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawRay(basePosition, transform.forward * 1.5f);
                        
                        // Draw rotation input indicator
                        Gizmos.color = Color.white;
                        Vector3 inputDirection = transform.right * _moveInput.x;
                        Gizmos.DrawRay(basePosition + Vector3.up * 0.2f, inputDirection * 0.8f);
                    }
                    else if (_moveDirection.magnitude > 0.01f)
                    {
                        // Draw current facing direction in yellow for movement-based rotation
                        Vector3 currentDirection = transform.forward;
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawRay(basePosition, currentDirection * 1.5f);
                    }
                    break;
                    
                case CharacterRotationMode.MouseControl:
                    // Draw current facing direction in cyan for mouse control
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawRay(basePosition, transform.forward * 1.5f);
                    
                    // Draw arc to show mouse sensitivity range
                    if (Application.isPlaying && _lookInput.magnitude > 0.01f)
                    {
                        Gizmos.color = Color.white;
                        float inputAngle = _lookInput.x * 30f; // Visual representation
                        Vector3 inputDirection = Quaternion.Euler(0f, transform.eulerAngles.y + inputAngle, 0f) * Vector3.forward;
                        Gizmos.DrawRay(basePosition, inputDirection * 1f);
                    }
                    break;
                    
                case CharacterRotationMode.MouseWithMovementFallback:
                    // Show current facing direction, color indicates control method
                    Gizmos.color = _isUsingMouseRotation ? Color.cyan : Color.yellow;
                    Gizmos.DrawRay(basePosition, transform.forward * 1.5f);
                    
                    // Show movement direction when available
                    if (_moveDirection.magnitude > 0.01f)
                    {
                        Gizmos.color = Color.yellow * 0.7f;
                        Gizmos.DrawRay(basePosition + Vector3.up * 0.2f, _moveDirection * 1.2f);
                    }
                    
                    // Show mouse activity indicator
                    if (_isUsingMouseRotation)
                    {
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireSphere(basePosition + Vector3.up * 0.3f, 0.1f);
                    }
                    break;
            }
        }
        #endregion
    }
}
