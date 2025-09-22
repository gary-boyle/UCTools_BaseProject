using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Camera;
using GameFramework.EventSystem.Events;
using UnityEngine.InputSystem;

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
            
            // Set the camera mount target for the camera component
            if (_cameraComponent != null && _cameraMount != null)
            {
                _cameraComponent.SetTarget(_cameraMount);
            }
            
            if (_showDebugInfo)
                Debug.Log("[FirstPersonController] Components initialized successfully");
        }

        #endregion


        #region Public Methods
        /// <summary>
        /// Set the camera mount transform (useful for external setup)
        /// </summary>
        public void SetCameraMount(Transform cameraMount)
        {
            _cameraMount = cameraMount;
            
            if (_cameraComponent != null)
            {
                _cameraComponent.SetTarget(cameraMount);
            }
        }

        /// <summary>
        /// Set the Cinemachine camera reference
        /// </summary>
        public void SetCinemachineCamera(CinemachineCamera camera)
        {
            _cinemachineCamera = camera;
        }

        /// <summary>
        /// Get the current camera mount transform
        /// </summary>
        public Transform GetCameraMount()
        {
            return _cameraMount;
        }

        /// <summary>
        /// Get the Cinemachine camera
        /// </summary>
        public CinemachineCamera GetCinemachineCamera()
        {
            return _cinemachineCamera;
        }

        /// <summary>
        /// Get reference to the first-person movement component
        /// </summary>
        public FirstPersonMovement GetFirstPersonMovement()
        {
            return _movementComponent;
        }

        /// <summary>
        /// Get reference to the first-person camera component
        /// </summary>
        public FirstPersonCameraControl GetFirstPersonCamera()
        {
            return _cameraComponent;
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
