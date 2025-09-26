using UnityEngine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Enum;
using GameFramework.Components.Controllers.Interfaces;
using Unity.Mathematics;

namespace GameFramework.Components.Controllers.Animation
{
    /// <summary>
    /// Unified animator controller that handles animation state updates for different player controller types.
    /// Supports both ThirdPerson and Isometric controllers with shared and controller-specific parameters.
    /// </summary>
    public class PlayerAnimatorController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Animation Components")]
        [SerializeField] private Animator _animator;
        
        [Header("Animation Parameters")]
        [SerializeField] private bool _useSpeedParameter = true;
        [SerializeField] private bool _useGroundedParameter = true;
        [SerializeField] private bool _useJumpingParameter = true;
        [SerializeField] private bool _useMovingParameter = true;
        [SerializeField] private bool _useCrouchingParameter = true;
        #endregion

        #region Animation Parameter Hashes
        // Common parameters
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
        
        // Animation parameters (used by both ThirdPerson and Isometric)
        private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
        private static readonly int IsJumpingParam = Animator.StringToHash("IsJumping");
        private static readonly int IsCrouchingParam = Animator.StringToHash("IsCrouching");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        #endregion

        #region Private Fields
        private IPlayerMovement _movementComponent;
        private bool _isInitialized = false;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            
            // Try to find movement component on parent or this gameobject
            _movementComponent = GetComponentInParent<IPlayerMovement>();
            if (_movementComponent == null)
            {
                _movementComponent = GetComponent<IPlayerMovement>();
            }
        }

        private void Start()
        {
            Initialize();
        }
        #endregion

        #region Initialization
        public void Initialize()
        {
            if (_animator == null)
            {
                Debug.LogError($"PlayerAnimatorController: No Animator found on {gameObject.name}");
                return;
            }

            if (_movementComponent == null)
            {
                Debug.LogError($"PlayerAnimatorController: No movement component found on {gameObject.name}");
                return;
            }

            _isInitialized = true;
        }

        public void Initialize(PlayerPrefabType controllerType, IPlayerMovement movementComponent, Animator animator = null)
        {
            _movementComponent = movementComponent;
            
            if (animator != null)
            {
                _animator = animator;
            }
            
            Initialize();
        }
        #endregion

        #region Animation Updates
        public void UpdateAnimations()
        {
            if (!_isInitialized || _animator == null || _movementComponent == null) return;

            Vector3 velocity = _movementComponent.CurrentVelocity;
            float speed = velocity.magnitude;

            // Update common parameters
            UpdateCommonParameters(velocity, speed);

            // Update shared animation parameters for both ThirdPerson and Isometric
            UpdateSharedParameters();
        }

        private void UpdateCommonParameters(Vector3 velocity, float speed)
        {
            if (_useSpeedParameter)
            {
                var remappedSpeed = math.remap(0f, _movementComponent.MoveSpeed, 0f, 1f, speed);
                _animator.SetFloat(SpeedParam, remappedSpeed);
            }
            
            if (_useMovingParameter)
            {
                bool isMoving = speed > 0.01f;
                _animator.SetBool(IsMovingParam, isMoving);
            }
        }

        private void UpdateSharedParameters()
        {
            // Set grounded state
            if (_useGroundedParameter)
            {
                _animator.SetBool(IsGroundedParam, _movementComponent.IsGrounded);
            }
            
            // Set jumping state
            if (_useJumpingParameter)
            {
                _animator.SetBool(IsJumpingParam, _movementComponent.IsJumping);
            }
            
            // Set crouching state
            if (_useCrouchingParameter)
            {
                _animator.SetBool(IsCrouchingParam, _movementComponent.IsCrouching);
            }
        }
        #endregion

        #region Animation Triggers
        public void TriggerAttack()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(AttackTrigger);
            }
        }

        #endregion

        #region Public Configuration
     

        public void SetMovementComponent(IPlayerMovement movementComponent)
        {
            _movementComponent = movementComponent;
        }

        public void SetAnimator(Animator animator)
        {
            _animator = animator;
        }

        public void EnableParameter(string parameterName, bool enable)
        {
            switch (parameterName.ToLower())
            {
                case "speed":
                    _useSpeedParameter = enable;
                    break;
                case "grounded":
                    _useGroundedParameter = enable;
                    break;
                case "jumping":
                    _useJumpingParameter = enable;
                    break;
                case "moving":
                    _useMovingParameter = enable;
                    break;
                case "crouching":
                    _useCrouchingParameter = enable;
                    break;
            }
        }
        #endregion

        #region Debug
        #endregion
    }
}
