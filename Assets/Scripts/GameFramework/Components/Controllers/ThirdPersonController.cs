using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Camera;
using GameFramework.Components.Controllers.Enum;
using GameFramework.EventSystem.Events;
using UnityEngine.InputSystem;

namespace GameFramework.Components.Controllers
{
    /// <summary>
    /// Third-person controller that combines third-person movement with orbital camera control.
    /// Uses Cinemachine 3.1+ for enhanced camera management with collision detection and smooth following.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class ThirdPersonController : BasePlayerController
    {
        #region Serialized Fields
        [Header("Movement Component")]
        [SerializeField] private ThirdPersonMovement _movementComponent;
        
        [Header("Camera Component")]
        [SerializeField] private ThirdPersonCameraControl _cameraComponent;
        
        [Header("Third Person Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraLookAtTarget;
        
        [Header("Character Model")]
        [SerializeField] private GameObject _characterModel;
        [SerializeField] private Animator _animator;
        #endregion

        #region Private Fields
        
        // Animation parameters
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
        private static readonly int IsJumpingParam = Animator.StringToHash("IsJumping");
        private static readonly int IsCrouchingParam = Animator.StringToHash("IsCrouching");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            // Set cursor lock requirement for third person controllers (can have camera control)
            _cursorLockRequirement = CursorLockRequirement.DuringGameplayWithUIExceptions;
            
            base.Awake();
            
            // Find components if not assigned
            if (_movementComponent == null) _movementComponent = GetComponent<ThirdPersonMovement>();
            if (_cameraComponent == null)  _cameraComponent = GetComponent<ThirdPersonCameraControl>();
            if (_cameraLookAtTarget == null) _cameraLookAtTarget = transform.Find("CameraLookAtTarget");
            if (_animator == null)  _animator = GetComponentInChildren<Animator>();
        }

        protected override void Update()
        {
            base.Update();
            
            if (_isInitialized && _animator != null)
            {
                UpdateAnimations();
            }
        }
        #endregion

        #region Component Creation
        protected override void CreateComponents()
        {
            // Assign the found components to the base class fields
            base._movementComponent = _movementComponent;
            base._cameraComponent = _cameraComponent;
        }
        
        protected override PlayerPrefabType GetControllerType()
        {
            return PlayerPrefabType.ThirdPerson;
        }

        #endregion

        #region Animation Updates
        private void UpdateAnimations()
        {
            if (_movementComponent == null) return;
            
            // Update speed parameter
            float speed = _movementComponent.CurrentVelocity.magnitude;
            _animator.SetFloat(SpeedParam, speed);
            
            // Update grounded state
            _animator.SetBool(IsGroundedParam, _movementComponent.IsGrounded);
            
            // Update jumping state (simplified - could be more sophisticated)
            bool isJumping = !_movementComponent.IsGrounded && _movementComponent.CurrentVelocity.y > 0.1f;
            _animator.SetBool(IsJumpingParam, isJumping);
        }
        #endregion
        

        #region Input Event Overrides
        
        /// <summary>
        /// Override to route look input to movement component for character rotation
        /// </summary>
        protected override void OnPlayerLookInput(PlayerLookInputEvent inputEvent)
        {
            if (!_isInitialized || !_isEnabled) return;
            
            // Route horizontal input to movement component for character rotation
            if (_movementComponent is ThirdPersonMovement thirdPersonMovement)
            {
                thirdPersonMovement.HandleLookInput(inputEvent);
            }
            
            // Still route to camera for any camera-specific handling (like vertical orbit)
            _cameraComponent?.HandleLookInput(inputEvent);
        }
        
        #endregion

        #region Debug
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            if (!_showDebugInfo) return;
            
            // Draw camera look-at target
            if (_cameraLookAtTarget != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_cameraLookAtTarget.position, 0.15f);
            }
        }
        #endregion
    }
}
