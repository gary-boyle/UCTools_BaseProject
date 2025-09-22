using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Camera;
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
            base.Awake();
            
            // Find components if not assigned
            if (_movementComponent == null)
            {
                _movementComponent = GetComponent<ThirdPersonMovement>();
            }
            
            if (_cameraComponent == null)
            {
                _cameraComponent = GetComponent<ThirdPersonCameraControl>();
            }
            
            // Find camera look-at target if not assigned
            if (_cameraLookAtTarget == null)
            {
                _cameraLookAtTarget = transform.Find("CameraLookAtTarget");
            }
            
            // Find animator if not assigned
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
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
            // Components are now assigned from inspector or found in Awake()
            if (_showDebugInfo)
                Debug.Log("[ThirdPersonController] Components initialized successfully");
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


        #region Public Methods
        /// <summary>
        /// Set the camera look-at target transform
        /// </summary>
        public void SetCameraLookAtTarget(Transform target)
        {
            _cameraLookAtTarget = target;
            
            if (_cameraComponent != null)
            {
                _cameraComponent.SetTarget(transform); // Still follow the main transform
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
        /// Set the character animator reference
        /// </summary>
        public void SetAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <summary>
        /// Set the character model GameObject
        /// </summary>
        public void SetCharacterModel(GameObject model)
        {
            _characterModel = model;
            
            // Try to find animator in the new model
            if (_animator == null && model != null)
            {
                _animator = model.GetComponent<Animator>();
            }
        }

        /// <summary>
        /// Get the current camera look-at target
        /// </summary>
        public Transform GetCameraLookAtTarget()
        {
            return _cameraLookAtTarget;
        }

        /// <summary>
        /// Get the Cinemachine camera
        /// </summary>
        public CinemachineCamera GetCinemachineCamera()
        {
            return _cinemachineCamera;
        }

        /// <summary>
        /// Get reference to the third-person movement component
        /// </summary>
        public ThirdPersonMovement GetThirdPersonMovement()
        {
            return _movementComponent;
        }

        /// <summary>
        /// Get reference to the third-person camera component
        /// </summary>
        public ThirdPersonCameraControl GetThirdPersonCamera()
        {
            return _cameraComponent;
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
