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
    public class IsometricMovement : BaseMovementComponent
    {
        #region Isometric Specific Fields
        [Header("Rotation Settings")]
        [SerializeField] private bool _use45DegreeOffset = true; // Enable for isometric camera alignment
        #endregion

        #region Isometric Specific Properties
        public bool Use45DegreeOffset => _use45DegreeOffset;
        
        /// <summary>
        /// Set move speed at runtime (used by grid movement configuration)
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = speed;
        }
        #endregion

        #region BaseMovementComponent Implementation
        public override void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            _moveInput = inputEvent.MovementVector;
        }

        protected override void UpdateMovementSpecific()
        {
            // Only update rotation when there's actual movement input to avoid jitter
            if (_moveInput.magnitude > 0.01f)
            {
                UpdateCharacterRotation();
            }
        }

        protected override void FixedUpdateMovementSpecific()
        {
            ApplyMovement();
        }
        #endregion

        #region Private Methods
        private void UpdateMovementDirection()
        {
            if (_moveInput.magnitude < 0.01f) return;

            // Convert 2D input to 3D direction
            Vector3 direction = new Vector3(_moveInput.x, 0f, _moveInput.y);
            
            // Apply 45-degree offset for isometric camera alignment
            if (_use45DegreeOffset)
            {
                direction = Quaternion.Euler(0f, 45f, 0f) * direction;
            }
            
            // Normalize for consistent movement speed
            direction = direction.normalized;
        }

        private void UpdateCharacterRotation()
        {
            if (_moveInput.magnitude < 0.01f) return;

            // Simple 4-directional rotation for isometric view
            Vector3 direction = new Vector3(_moveInput.x, 0f, _moveInput.y);
            
            if (_use45DegreeOffset)
            {
                direction = Quaternion.Euler(0f, 45f, 0f) * direction;
            }
            
            if (direction.magnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void ApplyMovement()
        {
            if (_moveInput.magnitude < 0.01f) return;

            Vector3 direction = new Vector3(_moveInput.x, 0f, _moveInput.y);
            
            if (_use45DegreeOffset)
            {
                direction = Quaternion.Euler(0f, 45f, 0f) * direction;
            }
            
            direction = direction.normalized;
            
            // Apply movement with effective speed
            float effectiveSpeed = GetEffectiveSpeed();
            Vector3 targetVelocity = direction * effectiveSpeed;
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            
            // Apply movement while preserving Y velocity
            _rigidbody.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
        }
        #endregion
    }
}