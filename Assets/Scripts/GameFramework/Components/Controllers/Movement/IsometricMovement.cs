using UnityEngine;
using GameFramework.EventSystem.Events;

namespace GameFramework.Components.Controllers.Movement
{
    /// <summary>
    /// Simple isometric movement component for top-down games.
    /// Handles basic movement with 4-directional rotation.
    /// Optional 45-degree offset for isometric camera alignment.
    /// </summary>
    public class IsometricMovement : BaseMovementComponent
    {
    #region Isometric Specific Fields
    [Header("Rotation Settings")]
    [SerializeField] private bool _use45DegreeOffset = true; // Enable for isometric camera alignment
    
    [Header("Grid Movement")]
    [SerializeField] private bool _useGridMovement = false; // Disabled for smooth classic isometric movement
    [SerializeField] private float _gridSize = 1.0f;
    [SerializeField] private float _gridMoveSpeed = 5.0f;
    #endregion
    
    #region Grid Movement Fields
    private Vector3 _gridTargetPosition;
    private bool _isMovingToGrid = false;
    private float _gridMoveStartTime;
    private LayerMask _interactionLayerMask = -1; // Will be set by controller
    #endregion
    
    #region Isometric Specific Properties
    public bool Use45DegreeOffset => _use45DegreeOffset;
    public bool UseGridMovement => _useGridMovement;
    
    /// <summary>
    /// Set move speed at runtime (used by grid movement configuration)
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        _moveSpeed = speed;
    }
    
    /// <summary>
    /// Configure grid movement settings
    /// </summary>
    public void ConfigureGridMovement(bool useGrid, float gridSize, float gridSpeed, LayerMask interactionMask)
    {
        _useGridMovement = useGrid;
        _gridSize = gridSize;
        _gridMoveSpeed = gridSpeed;
        _interactionLayerMask = interactionMask;
        
        if (_useGridMovement)
        {
            SetMoveSpeed(_gridMoveSpeed);
            _gridTargetPosition = SnapToGrid(transform.position);
            transform.position = _gridTargetPosition;
        }
    }
    #endregion

        #region BaseMovementComponent Implementation
        public override void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            
            if (_useGridMovement)
            {
                HandleGridMovement(inputEvent);
            }
            else
            {
                _moveInput = inputEvent.MovementVector;
            }
        }

        protected override void UpdateMovementSpecific()
        {
            UpdateGridMovement();
            
            // Only update rotation when there's actual movement input to avoid jitter
            if (_moveInput.magnitude > 0.01f)
            {
                UpdateCharacterRotation();
            }
        }

        protected override void FixedUpdateMovementSpecific()
        {
            ApplyMovement();
        }
        #endregion

        #region Private Methods

        private void UpdateCharacterRotation()
        {
            if (_moveInput.magnitude < 0.01f) return;

            // Simple 4-directional rotation for isometric view
            Vector3 direction = new Vector3(_moveInput.x, 0f, _moveInput.y);
            
            if (_use45DegreeOffset)
            {
                direction = Quaternion.Euler(0f, 45f, 0f) * direction;
            }
            
            if (direction.magnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void ApplyMovement()
        {
            if (_moveInput.magnitude < 0.01f) return;

            Vector3 direction = new Vector3(_moveInput.x, 0f, _moveInput.y);
            
            if (_use45DegreeOffset)
            {
                direction = Quaternion.Euler(0f, 45f, 0f) * direction;
            }
            
            direction = direction.normalized;
            
            // Apply movement with effective speed
            float effectiveSpeed = GetEffectiveSpeed();
            Vector3 targetVelocity = direction * effectiveSpeed;
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            
            // Apply movement while preserving Y velocity
            _rigidbody.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);
        }
        #endregion
        
        #region Grid Movement Methods
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
            
            // Apply 45-degree offset if enabled
            if (_use45DegreeOffset)
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
                transform.position += direction * (_gridMoveSpeed * Time.deltaTime);
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
        
        /// <summary>
        /// Draw debug gizmos for grid movement (called from controller)
        /// </summary>
        public void DrawGridDebugGizmos(bool showDebugInfo)
        {
            if (!showDebugInfo || !_useGridMovement) return;
            
            // Draw grid
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
        #endregion
    }
}