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
    public class ThirdPersonMovement : BaseMovementComponent
    {
        #region Third Person Specific Fields
        [Header("Third Person Movement")]
        [SerializeField] private float _airControl = 0.3f;
        
        [Header("Rotation Settings")]
        [SerializeField] private CharacterRotationSettings _rotationSettings = new CharacterRotationSettings();
        #endregion

        #region Third Person Specific Private Fields
        private UnityEngine.Camera _mainCamera;
        private Vector3 _moveDirection = Vector3.zero;
        
        // Input state
        private Vector2 _lookInput = Vector2.zero;
        private float _currentYaw = 0f;
        private float _timeSinceLastMouseInput = 0f;
        
        // Hybrid mode state
        private bool _isUsingMouseRotation = false;
        #endregion

        #region Third Person Specific Properties
        public Transform MovementTransform => transform;
        public float CurrentYaw => _currentYaw;
        public CharacterRotationMode RotationMode => _rotationSettings.rotationMode;
        public bool IsUsingMouseRotation => _isUsingMouseRotation;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake(); // Get common components and services
            
            // Third person specific initialization
            _mainCamera = UnityEngine.Camera.main;
        }

        #endregion

        #region BaseMovementComponent Implementation
        public override void Initialize()
        {
            base.Initialize(); // Call base initialization
            
            if (_mainCamera == null)
            {
                Debug.LogWarning($"[ThirdPersonMovement] No main camera found. Movement will be relative to world space.");
            }
            
            // Initialize current yaw from transform
            _currentYaw = transform.eulerAngles.y;
        }

        public override void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            _moveInput = inputEvent.MovementVector;
        }
        
        protected override void UpdateMovementSpecific()
        {
            UpdateMouseInactivityTimer();
        }

        protected override void FixedUpdateMovementSpecific()
        {
            HandleMovement();
        }

        public override void StopMovement()
        {
            base.StopMovement(); // Clear base movement state
            
            // Clear third person specific state
            _moveDirection = Vector3.zero;
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
            
            // Process rotation input immediately
            ProcessRotationInput();
        }
        #endregion
        
        #region Rotation
        /// <summary>
        /// Changes the character's rotation mode at runtime
        /// </summary>
        public void SetRotationMode(CharacterRotationMode mode)
        {
            _rotationSettings.rotationMode = mode;
            
            // Reset state when switching modes
            _timeSinceLastMouseInput = 0f;
            _isUsingMouseRotation = false;
            
        }
        
        /// <summary>
        /// Gets the current rotation settings (read-only access)
        /// </summary>
        public CharacterRotationSettings GetRotationSettings()
        {
            return _rotationSettings;
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

        /// <summary>
        /// Process rotation input immediately when received (input-dependent logic)
        /// </summary>
        private void ProcessRotationInput()
        {
            switch (_rotationSettings.rotationMode)
            {
                case CharacterRotationMode.None:
                    // No rotation updates
                    break;
                    
                case CharacterRotationMode.TankControls:
                    // Tank controls don't use mouse look input, only A/D keys
                    break;
                    
                case CharacterRotationMode.MouseControl:
                    HandleMouseRotation();
                    break;
                    
                case CharacterRotationMode.MouseWithMovementFallback:
                    HandleHybridRotation();
                    break;
            }
        }
        
        /// <summary>
        /// Update time-dependent rotation logic in Update loop
        /// </summary>
        private void UpdateMouseInactivityTimer()
        {
            // Update mouse inactivity timer
            _timeSinceLastMouseInput += Time.deltaTime;
            
            // Handle tank controls rotation with A/D keys (time-dependent, not input-event dependent)
            if (_rotationSettings.rotationMode == CharacterRotationMode.TankControls)
            {
                HandleMovementDirectionRotation();
            }
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
                
            }
            // When moving diagonally (W+A, W+D), A/D is used for movement, not rotation
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
