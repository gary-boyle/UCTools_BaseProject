using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Camera;

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
        [Header("Movement Component")]
        [SerializeField] private FirstPersonMovement _movementComponent;
        
        [Header("Camera Component")]
        [SerializeField] private FirstPersonCameraControl _cameraComponent;
        
        [Header("First Person Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraMount;

        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            // Find components if not assigned
            if (_movementComponent == null)
            {
                _movementComponent = GetComponent<FirstPersonMovement>();
            }
            
            if (_cameraComponent == null)
            {
                _cameraComponent = GetComponent<FirstPersonCameraControl>();
            }
            
            // Find camera mount if not assigned
            if (_cameraMount == null)
            {
                _cameraMount = transform.Find("CameraMount");
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
