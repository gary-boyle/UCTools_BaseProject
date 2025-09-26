using UnityEngine;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.Components.Controllers.Enum;

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
        
        #endregion

        #region BaseMovementComponent Implementation
        public override void Initialize()
        {
            base.Initialize(); // Call base initialization
            
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
            // noop
        }


        protected override void FixedUpdateMovementSpecific()
        {
            HandleMovement();
            ProcessRotationInput();
        }
        
        public void HandleLookInput(PlayerLookInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;

            _lookInput = inputEvent.LookDelta;
        }
        #endregion

        #region Private Methods

        private void HandleMovement()
        {
            // Determine effective movement input based on rotation mode
            Vector2 effectiveInput = _moveInput;
            
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
            }
        }

        private void HandleMouseRotation()
        {
            if (_lookInput.magnitude < 0.001f) return;
            
            // Process horizontal mouse input for character rotation
            float horizontalInput = _lookInput.x * _rotationSettings.mouseRotationSensitivity;
            
            // Apply mouse rotation directly
            _currentYaw += horizontalInput;
            transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
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
            }
        }
        #endregion
    }
}
