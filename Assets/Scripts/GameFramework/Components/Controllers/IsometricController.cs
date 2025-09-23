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
    /// Isometric controller that combines top-down character movement with fixed-angle isometric camera.
    /// Suitable for isometric RPGs, puzzle games, and top-down action games.
    /// Uses Cinemachine 3.1+ for enhanced camera management.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class IsometricController : BasePlayerController
    {
        #region Serialized Fields
        [Header("Isometric Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _cameraLookAtTarget;
        
        [Header("Movement Component")]
        [SerializeField] private IsometricMovement _movementComponent;
        
        [Header("Camera Component")]
        [SerializeField] private IsometricCameraControl _cameraComponent;
        
        [Header("Animation")]
        [SerializeField] private PlayerAnimatorController _animatorController;
        
        [Header("Character Model")]
        [SerializeField] private GameObject _characterModel;
        
        [Header("Grid Movement")]
        [SerializeField] private bool _useGridMovement = false; // Disabled for smooth classic isometric movement
        [SerializeField] private float _gridSize = 1.0f;
        [SerializeField] private float _gridMoveSpeed = 5.0f;
        #endregion

        #region Private Fields
        // Grid movement
        private Vector3 _gridTargetPosition;
        private bool _isMovingToGrid = false;
        private float _gridMoveStartTime;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            // Set cursor lock requirement for isometric controllers (never lock cursor)
            _cursorLockRequirement = CursorLockRequirement.Never;
            
            base.Awake();
            
            // Find components if not assigned
            if (_movementComponent == null) _movementComponent = GetComponent<IsometricMovement>();
            if (_cameraComponent == null) _cameraComponent = GetComponent<IsometricCameraControl>();
            if (_animatorController == null) _animatorController = GetComponentInChildren<PlayerAnimatorController>();
            
            // Initialize grid position
            if (_useGridMovement)
            {
                _gridTargetPosition = SnapToGrid(transform.position);
                transform.position = _gridTargetPosition;
            }
        }

        protected override void Update()
        {
            base.Update();
            
            if (_isInitialized)
            {
                if (_animatorController != null)
                {
                    _animatorController.UpdateAnimations();
                }
                UpdateGridMovement();
            }
        }
        #endregion

        #region Component Setup
        protected override void CreateComponents()
        {
            // Assign the found components to the base class fields
            base._movementComponent = _movementComponent;
            base._cameraComponent = _cameraComponent;

            // Initialize animator controller
            if (_animatorController != null && _movementComponent != null)
            {
                Animator animator = GetComponentInChildren<Animator>();
                _animatorController.Initialize(PlayerPrefabType.Isometric, _movementComponent, animator);
            }

            // Components are now assigned from inspector or found in Awake()
            if (_movementComponent != null)
            {
                // Configure for grid movement if enabled
                if (_useGridMovement)
                {
                    //_movementComponent.SetUsePhysics(false); // Grid movement typically doesn't use physics
                    _movementComponent.SetMoveSpeed(_gridMoveSpeed);
                }
            }
        }
        
        protected override PlayerPrefabType GetControllerType()
        {
            return PlayerPrefabType.Isometric;
        }

        #endregion

        #region Movement Handling
        protected override void OnPlayerMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (_useGridMovement)
            {
                HandleGridMovement(inputEvent);
            }
            else
            {
                base.OnPlayerMoveInput(inputEvent);
            }
        }

        private void HandleGridMovement(PlayerMoveInputEvent inputEvent)
        {
            if (_isMovingToGrid) return; // Ignore input while moving
            
            Vector2 input = inputEvent.MovementVector;
            if (input.magnitude < 0.5f) return; // Ignore small inputs
            
            // Convert input to grid direction
            Vector3 direction = Vector3.zero;
            
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                // Horizontal movement
                direction = input.x > 0 ? Vector3.right : Vector3.left;
            }
            else
            {
                // Vertical movement  
                direction = input.y > 0 ? Vector3.forward : Vector3.back;
            }
            
            // Apply 45-degree offset if the movement component uses it
            if (_movementComponent != null && _movementComponent.Use45DegreeOffset)
            {
                direction = Quaternion.Euler(0f, 45f, 0f) * direction;
            }
            
            // Calculate target position
            Vector3 targetPos = transform.position + direction * _gridSize;
            targetPos = SnapToGrid(targetPos);
            
            // Check if target position is valid
            if (IsValidGridPosition(targetPos))
            {
                _gridTargetPosition = targetPos;
                _isMovingToGrid = true;
                _gridMoveStartTime = Time.time;
            }
        }

        private void UpdateGridMovement()
        {
            if (!_useGridMovement || !_isMovingToGrid) return;
            
            float elapsedTime = Time.time - _gridMoveStartTime;
            float moveDistance = _gridMoveSpeed * elapsedTime;
            float totalDistance = Vector3.Distance(transform.position, _gridTargetPosition);
            
            if (moveDistance >= totalDistance)
            {
                // Movement complete
                transform.position = _gridTargetPosition;
                _isMovingToGrid = false;
            }
            else
            {
                // Continue moving
                Vector3 direction = (_gridTargetPosition - transform.position).normalized;
                transform.position += direction * _gridMoveSpeed * Time.deltaTime;
            }
        }

        private Vector3 SnapToGrid(Vector3 worldPos)
        {
            float snappedX = Mathf.Round(worldPos.x / _gridSize) * _gridSize;
            float snappedZ = Mathf.Round(worldPos.z / _gridSize) * _gridSize;
            return new Vector3(snappedX, worldPos.y, snappedZ);
        }

        private bool IsValidGridPosition(Vector3 gridPos)
        {
            // Check for obstacles using a small sphere cast
            return !Physics.CheckSphere(gridPos, 0.4f, ~_interactionLayerMask);
        }
        #endregion

        #region Animation Control
        /// <summary>
        /// Get access to the animator controller for advanced animation control
        /// </summary>
        public PlayerAnimatorController AnimatorController => _animatorController;
        #endregion

        #region Debug
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            if (!_showDebugInfo) return;
            
            // Draw grid
            if (_useGridMovement)
            {
                Gizmos.color = Color.white;
                Vector3 pos = transform.position;
                float gridRange = 10f;
                
                for (float x = -gridRange; x <= gridRange; x += _gridSize)
                {
                    for (float z = -gridRange; z <= gridRange; z += _gridSize)
                    {
                        Vector3 gridPos = new Vector3(pos.x + x, pos.y, pos.z + z);
                        Gizmos.DrawWireCube(gridPos, Vector3.one * 0.1f);
                    }
                }
                
                // Draw target position
                if (_isMovingToGrid)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(_gridTargetPosition, Vector3.one * 0.3f);
                }
            }
        }
        #endregion
    }

}
