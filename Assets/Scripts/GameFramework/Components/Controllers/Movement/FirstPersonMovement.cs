using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using UnityEngine.InputSystem;
using System.Collections;

namespace GameFramework.Components.Controllers.Movement
{
    /// <summary>
    /// First-person movement component that handles WASD movement, jumping, sprinting, and crouching.
    /// Uses Rigidbody physics for realistic movement.
    /// </summary>
    public class FirstPersonMovement : BaseMovementComponent
    {
    #region First Person Specific Fields
    [Header("First Person Movement")]
    [SerializeField] private float _airControl = 0.5f;
    
    [Header("Crouching")]
    [SerializeField] private float _crouchHeight = 1.0f;
    [SerializeField] private float _standingHeight = 2.0f;
    [SerializeField] private float _crouchTransitionSpeed = 5.0f;
    #endregion

        #region First Person Specific Private Fields
        private Vector3 _moveDirection = Vector3.zero;
        #endregion

        #region BaseMovementComponent Implementation
        public override void Initialize()
        {
            base.Initialize();
            
            // Set initial collider height to standing height
            if (_collider != null)
            {
                _collider.height = _standingHeight;
                _collider.center = new Vector3(_collider.center.x, _standingHeight / 2f, _collider.center.z);
            }
        }
        
        public override void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            _moveInput = inputEvent.MovementVector;
        }

        protected override void UpdateMovementSpecific()
        {
            UpdateMovementDirection();
        }

        protected override void FixedUpdateMovementSpecific()
        {
            ApplyMovement();
        }
        #endregion

        #region Private Methods
        private void UpdateMovementDirection()
        {
            // Convert input to world-space movement direction
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            _moveDirection = (forward * _moveInput.y + right * _moveInput.x).normalized;
        }

        private void ApplyMovement()
        {
            float effectiveSpeed = GetEffectiveSpeed();
            Vector3 targetVelocity = _moveDirection * effectiveSpeed;
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
        /// Handle crouch input with smooth height transition for first-person
        /// </summary>
        public override void HandleCrouchInput(PlayerCrouchInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            if (inputEvent.Phase == InputActionPhase.Performed)
            {
                _isCrouching = !_isCrouching;
                StartCoroutine(TransitionCrouchHeight());
            }
        }
        
        /// <summary>
        /// Smoothly transitions between crouch and standing height for first-person
        /// </summary>
        private IEnumerator TransitionCrouchHeight()
        {
            float targetHeight = _isCrouching ? _crouchHeight : _standingHeight;
            float startHeight = _collider.height;
            float startCenterY = _collider.center.y;
            float targetCenterY = targetHeight / 2f;
            
            float elapsedTime = 0f;
            float transitionDuration = 1f / _crouchTransitionSpeed;
            
            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / transitionDuration;
                
                // Smooth interpolation
                _collider.height = Mathf.Lerp(startHeight, targetHeight, t);
                //_collider.center = new Vector3(_collider.center.x, Mathf.Lerp(startCenterY, targetCenterY, t), _collider.center.z);
                
                yield return null;
            }
            
            // Ensure final values are exact
            _collider.height = targetHeight;
            //_collider.center = new Vector3(_collider.center.x, targetCenterY, _collider.center.z);
        }

        #endregion
    }
}