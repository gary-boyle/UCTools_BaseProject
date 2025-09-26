using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using System.Collections;

namespace GameFramework.Components.Controllers.Movement
{
    /// <summary>
    /// Base class for all movement components providing common functionality.
    /// Handles physics setup, ground detection, jumping, crouching, and basic state management.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public abstract class BaseMovementComponent : MonoBehaviour, IPlayerMovement
    {
        #region Common Serialized Fields
        [Header("Base Movement Settings")]
        [SerializeField] protected float _moveSpeed = 5.0f;
        [SerializeField] protected float _sprintMultiplier = 1.5f;
        [SerializeField] protected float _crouchMultiplier = 0.5f;
        [SerializeField] protected float _jumpForce = 5.0f;
        
        [Header("Ground Detection")]
        [SerializeField] protected LayerMask _groundLayerMask = 1;
        [SerializeField] protected float _groundCheckDistance = 0.1f;
        [SerializeField] protected Transform _groundCheckPoint;

        [Header("Debug")]
        [SerializeField] protected bool _showDebugInfo = false;
        #endregion

        #region Common Protected Fields
        protected Rigidbody _rigidbody;
        protected CapsuleCollider _collider;
        protected IPauseService _pauseService;
        
        // Movement state
        protected Vector2 _moveInput = Vector2.zero;
        protected bool _isJumpRequested = false;
        protected bool _isSprinting = false;
        protected bool _isCrouching = false;
        protected bool _isGrounded = false;
        protected bool _isInitialized = false;
        
        #endregion

        #region Common Properties
        public virtual bool IsPaused => _pauseService?.IsPaused ?? false;
        public virtual Vector3 CurrentVelocity => _rigidbody?.linearVelocity ?? Vector3.zero;
        public virtual bool IsGrounded => _isGrounded;
        public virtual bool IsCrouching => _isCrouching;
        public virtual bool IsJumping => _rigidbody != null && _rigidbody.linearVelocity.y > 0.1f;
        public virtual float MoveSpeed => _moveSpeed;
        #endregion

        #region Common Unity Lifecycle
        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();
            _pauseService = GameManager.GetService<IPauseService>();
        }

        #endregion

        #region Common Interface Implementation
        public virtual void Initialize()
        {
            if (_isInitialized) return;
            
            ValidateComponents();
            SetupInitialState();
            
            _isInitialized = true;
        }

        public virtual void Cleanup()
        {
            _isInitialized = false;
        }

        public virtual void HandleJumpInput(PlayerJumpInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            if (inputEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Performed && _isGrounded)
            {
                _isJumpRequested = true;
            }
        }

        public virtual void HandleSprintInput(PlayerSprintInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            _isSprinting = inputEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Performed;
        }

        public virtual void HandleCrouchInput(PlayerCrouchInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            // Default implementation - override in specific movement components if crouch is needed
        }

        public virtual void StopMovement()
        {
            _moveInput = Vector2.zero;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = new Vector3(0, _rigidbody.linearVelocity.y, 0);
            }
        }

        public virtual void UpdateMovement()
        {
            if (!_isInitialized || IsPaused) return;
            
            UpdateGroundedState();
            UpdateMovementSpecific();
        }

        public virtual void FixedUpdateMovement()
        {
            if (!_isInitialized || IsPaused) return;
            
            HandleJump();
            FixedUpdateMovementSpecific();
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Handle movement input - implemented by specific movement types
        /// </summary>
        public abstract void HandleMoveInput(PlayerMoveInputEvent inputEvent);
        
        /// <summary>
        /// Update movement logic specific to this movement type
        /// </summary>
        protected abstract void UpdateMovementSpecific();
        
        /// <summary>
        /// Fixed update movement logic specific to this movement type
        /// </summary>
        protected abstract void FixedUpdateMovementSpecific();
        #endregion

        #region Common Protected Methods
        /// <summary>
        /// Updates grounded state using raycast
        /// </summary>
        protected virtual void UpdateGroundedState()
        {
            Vector3 checkPosition = _groundCheckPoint != null ? _groundCheckPoint.position : transform.position;
            _isGrounded = Physics.Raycast(checkPosition, Vector3.down, 
                _collider.bounds.extents.y + _groundCheckDistance, _groundLayerMask);
        }

        /// <summary>
        /// Handles jump physics
        /// </summary>
        protected virtual void HandleJump()
        {
            if (_isJumpRequested && _isGrounded && _rigidbody != null)
            {
                _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                _isJumpRequested = false;
            }
        }


        /// <summary>
        /// Gets current effective movement speed based on modifiers
        /// </summary>
        protected virtual float GetEffectiveSpeed()
        {
            float currentSpeed = _moveSpeed;
            if (_isSprinting) currentSpeed *= _sprintMultiplier;
            if (_isCrouching) currentSpeed *= _crouchMultiplier;
            return currentSpeed;
        }
        

        /// <summary>
        /// Validates required components are present
        /// </summary>
        private void ValidateComponents()
        {
            if (_rigidbody == null || _collider == null)
            {
                Debug.LogError($"[{GetType().Name}] Required components missing on {gameObject.name}. Movement will be disabled.");
                enabled = false;
                return;
            }

            if (_pauseService == null)
            {
                Debug.LogWarning($"[{GetType().Name}] IPauseService not found. Pause functionality will not work.");
            }
        }

        /// <summary>
        /// Sets up initial component state
        /// </summary>
        private void SetupInitialState()
        {
            // Configure rigidbody for character movement
            _rigidbody.freezeRotation = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
        #endregion

        #region Virtual Debug Methods
        protected virtual void OnDrawGizmos()
        {
            if (!_showDebugInfo) return;
            
            // Draw ground check
            if (_groundCheckPoint != null)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawRay(_groundCheckPoint.position, Vector3.down * _groundCheckDistance);
                Gizmos.DrawWireSphere(_groundCheckPoint.position + Vector3.down * _groundCheckDistance, 0.1f);
            }
        }
        #endregion
    }
}
