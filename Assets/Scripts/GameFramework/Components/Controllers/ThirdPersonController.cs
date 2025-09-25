using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Camera;
using GameFramework.Components.Controllers.Animation;
using GameFramework.Components.Controllers.Enum;
using GameFramework.EventSystem.Events;

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
        [Header("Third Person Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraLookAtTarget;
        
        [Header("Character Model")]
        [SerializeField] private GameObject _characterModel;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            // Set cursor lock requirement for third person controllers (can have camera control)
            _cursorLockRequirement = CursorLockRequirement.DuringGameplayWithUIExceptions;
            
            base.Awake();
        }
        #endregion

        #region Component Management
        protected override void FindComponents()
        {
            base.FindComponents(); // Find common components (animation controller)
            
            // Find third person specific components
            if (_cameraLookAtTarget == null) 
                _cameraLookAtTarget = transform.Find("CameraLookAtTarget");
        }

        protected override void CreateComponents()
        {
            // Find and assign movement component
            var movementComponent = GetComponent<ThirdPersonMovement>();
            if (movementComponent == null)
            {
                Debug.LogError($"[{GetType().Name}] ThirdPersonMovement component not found on {gameObject.name}");
                return;
            }
            
            // Find and assign camera component  
            var cameraComponent = GetComponent<ThirdPersonCameraControl>();
            if (cameraComponent == null)
            {
                Debug.LogError($"[{GetType().Name}] ThirdPersonCameraControl component not found on {gameObject.name}");
                return;
            }

            // Assign to base class fields
            _movementComponent = movementComponent;
            _cameraComponent = cameraComponent;
        }
        
        protected override PlayerPrefabType GetControllerType()
        {
            return PlayerPrefabType.ThirdPerson;
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
            
            // Route full input to camera for vertical orbit and zoom handling
            // Camera will only use the vertical component for orbit
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