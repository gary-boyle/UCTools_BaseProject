using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Camera;
using GameFramework.Components.Controllers.Enum;

namespace GameFramework.Components.Controllers
{
    /// <summary>
    /// First-person controller that combines first-person movement with direct camera control.
    /// Uses Cinemachine 3.1+ for enhanced camera management.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class FirstPersonController : BasePlayerController
    {
        #region Serialized Fields
        [Header("First Person Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraMount;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            // Set cursor lock requirement for first person controllers
            _cursorLockRequirement = CursorLockRequirement.DuringGameplay;
            
            base.Awake();
        }
        #endregion

        #region Component Management
        protected override void FindComponents()
        {
            base.FindComponents(); // Find common components (animation controller)
            
            // Find camera mount if not assigned
            if (_cameraMount == null)
            {
                _cameraMount = transform.Find("CameraMount");
            }
        }

        protected override void CreateComponents()
        {
            // Find and assign movement component
            var movementComponent = GetComponent<FirstPersonMovement>();
            if (movementComponent == null)
            {
                Debug.LogError($"[{GetType().Name}] FirstPersonMovement component not found on {gameObject.name}");
                return;
            }
            
            // Find and assign camera component  
            var cameraComponent = GetComponent<FirstPersonCameraControl>();
            if (cameraComponent == null)
            {
                Debug.LogError($"[{GetType().Name}] FirstPersonCameraControl component not found on {gameObject.name}");
                return;
            }

            // Assign to base class fields
            _movementComponent = movementComponent;
            _cameraComponent = cameraComponent;
        }
        
        protected override PlayerPrefabType GetControllerType()
        {
            return PlayerPrefabType.FPS;
        }
        #endregion

        #region Debug
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            if (!_showDebugInfo || _cameraMount == null) return;
            
            // Draw camera mount position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(_cameraMount.position, 0.1f);
        }
        #endregion
    }
}