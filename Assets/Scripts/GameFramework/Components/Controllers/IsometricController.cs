using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Movement;
using GameFramework.Components.Controllers.Camera;
using GameFramework.Components.Controllers.Enum;
using GameFramework.EventSystem.Events;
using System.Collections.Generic;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using UnityEngine.InputSystem;

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
        
        [Header("Character Model")]
        [SerializeField] private GameObject _characterModel;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _characterSprite; // For sprite-based characters
        
        [Header("Grid Movement")]
        [SerializeField] private bool _useGridMovement = false; // Disabled for smooth classic isometric movement
        [SerializeField] private float _gridSize = 1.0f;
        [SerializeField] private float _gridMoveSpeed = 5.0f;
        
        [Header("Sprite Settings")]
        [SerializeField] private bool _useSpriteRenderer = false;
        [SerializeField] private bool _flipSpriteWithMovement = true;
        #endregion

        #region Private Fields
        // Grid movement
        private Vector3 _gridTargetPosition;
        private bool _isMovingToGrid = false;
        private float _gridMoveStartTime;
        
        // Sprite handling
        private bool _lastMovementWasRight = true;
        
        // Animation parameters
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int HorizontalParam = Animator.StringToHash("Horizontal");
        private static readonly int VerticalParam = Animator.StringToHash("Vertical");
        private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
        private static readonly int DirectionParam = Animator.StringToHash("Direction");
        
        // Direction constants for 8-directional animation
        private const int DIR_SOUTH = 0;
        private const int DIR_SOUTH_WEST = 1;
        private const int DIR_WEST = 2;
        private const int DIR_NORTH_WEST = 3;
        private const int DIR_NORTH = 4;
        private const int DIR_NORTH_EAST = 5;
        private const int DIR_EAST = 6;
        private const int DIR_SOUTH_EAST = 7;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            // Set cursor lock requirement for isometric controllers (never lock cursor)
            _cursorLockRequirement = CursorLockRequirement.Never;
            
            base.Awake();
            
            // Find components if not assigned
            if (_movementComponent == null)
            {
                _movementComponent = GetComponent<IsometricMovement>();
            }
            
            if (_cameraComponent == null)
            {
                _cameraComponent = GetComponent<IsometricCameraControl>();
            }
            
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            
            if (_characterSprite == null)
            {
                _characterSprite = GetComponentInChildren<SpriteRenderer>();
            }
            
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
                UpdateAnimations();
                UpdateGridMovement();
                UpdateSpriteFlipping();
            }
        }
        #endregion

        #region Component Setup
        protected override void CreateComponents()
        {
            // Assign the found components to the base class fields
            base._movementComponent = _movementComponent;
            base._cameraComponent = _cameraComponent;

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
            
            if (_showDebugInfo)
                Debug.Log("[IsometricController] Components initialized successfully");
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

        #region Animation Updates
        private void UpdateAnimations()
        {
            if (_animator == null) return;
            
            Vector3 velocity = _movementComponent?.CurrentVelocity ?? Vector3.zero;
            
            // Update movement parameters
            _animator.SetFloat(SpeedParam, velocity.magnitude);
            _animator.SetFloat(HorizontalParam, velocity.x);
            _animator.SetFloat(VerticalParam, velocity.z);
            _animator.SetBool(IsMovingParam, velocity.magnitude > 0.1f);
            
            // Update 8-directional animation
            if (velocity.magnitude > 0.1f)
            {
                int direction = GetDirectionFromVelocity(velocity);
                _animator.SetInteger(DirectionParam, direction);
            }
        }

        private int GetDirectionFromVelocity(Vector3 velocity)
        {
            float angle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            
            // Convert angle to 8-direction index
            angle += 22.5f; // Offset for rounding
            int direction = (int)(angle / 45f) % 8;
            
            return direction;
        }

        private void UpdateSpriteFlipping()
        {
            if (!_useSpriteRenderer || _characterSprite == null || !_flipSpriteWithMovement) return;
            
            Vector3 velocity = _movementComponent?.CurrentVelocity ?? Vector3.zero;
            
            if (Mathf.Abs(velocity.x) > 0.1f)
            {
                _lastMovementWasRight = velocity.x > 0;
                _characterSprite.flipX = !_lastMovementWasRight;
            }
        }        
        #endregion

        #region Input Event Overrides
        /// <summary>
        /// Override to disable look input for locked camera - isometric games typically have fixed cameras
        /// </summary>
        protected override void OnPlayerLookInput(PlayerLookInputEvent inputEvent)
        {
            // Disabled - isometric camera should be locked, player rotates instead
            // No input routing needed for classic isometric gameplay
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
                _cameraComponent.SetTarget(target);
            }
        }

        /// <summary>
        /// Set the Cinemachine camera reference
        /// </summary>
        public void SetCinemachineCamera(CinemachineCamera camera)
        {
            _cinemachineCamera = camera;
            
            // Update the camera component if it exists
            if (_cameraComponent != null)
            {
                _cameraComponent.SetTarget(transform);
            }
        }

        /// <summary>
        /// Set whether to use grid-based movement
        /// </summary>
        public void SetGridMovement(bool useGrid, float gridSize = 1.0f)
        {
            _useGridMovement = useGrid;
            _gridSize = gridSize;
            
            // if (_movementComponent != null)
            // {
            //     _movementComponent.SetUsePhysics(!useGrid);
            // }
            
            if (useGrid)
            {
                _gridTargetPosition = SnapToGrid(transform.position);
                transform.position = _gridTargetPosition;
            }
        }

        /// <summary>
        /// Set the character animator
        /// </summary>
        public void SetAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <summary>
        /// Set the character sprite renderer
        /// </summary>
        public void SetSpriteRenderer(SpriteRenderer spriteRenderer)
        {
            _characterSprite = spriteRenderer;
        }

        /// <summary>
        /// Get reference to the isometric movement component
        /// </summary>
        public IsometricMovement GetIsometricMovement()
        {
            return _movementComponent;
        }

        /// <summary>
        /// Get reference to the isometric camera component
        /// </summary>
        public IsometricCameraControl GetIsometricCamera()
        {
            return _cameraComponent;
        }

        /// <summary>
        /// Check if currently moving in grid mode
        /// </summary>
        public bool IsMovingToGrid()
        {
            return _isMovingToGrid;
        }
        
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
