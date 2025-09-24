using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Camera;
using GameFramework.Components.Controllers.Animation;
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
        
        [Header("Animation")]
        [SerializeField] private PlayerAnimatorController _animatorController;
        
        [Header("Character Model")]
        [SerializeField] private GameObject _characterModel;
        #endregion

        #region Private Fields
        // Private fields for component references and state
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
            if (_animatorController == null) _animatorController = GetComponentInChildren<PlayerAnimatorController>();
        }

        protected override void Update()
        {
            base.Update();
            
            if (_isInitialized && _animatorController != null)
            {
                _animatorController.UpdateAnimations();
            }
        }
        #endregion

        #region Component Creation
        protected override void CreateComponents()
        {
            // Assign the found components to the base class fields
            base._movementComponent = _movementComponent;
            base._cameraComponent = _cameraComponent;
            
            // Initialize animator controller
            if (_animatorController != null && _movementComponent != null)
            {
                Animator animator = GetComponentInChildren<Animator>();
                _animatorController.Initialize(PlayerPrefabType.ThirdPerson, _movementComponent, animator);
            }
        }
        
        protected override PlayerPrefabType GetControllerType()
        {
            return PlayerPrefabType.ThirdPerson;
        }

        #endregion

        #region Animation Control
        /// <summary>
        /// Trigger attack animation
        /// </summary>
        public void TriggerAttack()
        {
            _animatorController?.TriggerAttack();
        }
        
        
        /// <summary>
        /// Get access to the animator controller for advanced animation control
        /// </summary>
        public PlayerAnimatorController AnimatorController => _animatorController;
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
