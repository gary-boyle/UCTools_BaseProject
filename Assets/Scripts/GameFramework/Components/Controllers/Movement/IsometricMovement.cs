using UnityEngine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using UnityEngine.InputSystem;

namespace GameFramework.Components.Controllers.Movement
{
    /// <summary>
    /// Simple isometric movement component for top-down games.
    /// Handles basic movement with 4-directional rotation (North, South, East, West).
    /// </summary>
    public class IsometricMovement : MonoBehaviour, IPlayerMovement
    {
        #region Serialized Fields
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5.0f;
        #endregion

        #region Private Fields
        private IPauseService _pauseService;
        
        // Movement state
        private Vector2 _moveInput = Vector2.zero;
        private bool _isInitialized = false;
        
        // 4-directional movement angles (North, East, South, West)
        private readonly float[] _cardinalAngles = { 0f, 90f, 180f, 270f };
        #endregion

        #region Public Properties
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        public Transform MovementTransform => transform;
        public Vector3 CurrentVelocity => Vector3.zero;
        public bool IsGrounded => true;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _pauseService = GameManager.GetService<IPauseService>();
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            UpdateMovement();
        }
        #endregion

        #region IPlayerMovement Implementation
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
        }

        public void Cleanup()
        {
            _pauseService = null;
            _isInitialized = false;
        }

        public void HandleMoveInput(PlayerMoveInputEvent inputEvent)
        {
            if (!_isInitialized || IsPaused) return;
            _moveInput = inputEvent.MovementVector;
        }

        public void HandleJumpInput(PlayerJumpInputEvent inputEvent)
        {
            // No jump in isometric movement
        }

        public void HandleSprintInput(PlayerSprintInputEvent inputEvent)
        {
            // No sprint in simplified version
        }

        public void HandleCrouchInput(PlayerCrouchInputEvent inputEvent)
        {
            // No crouch in simplified version
        }

        public void UpdateMovement()
        {
            if (!_isInitialized || IsPaused) return;
            
            HandleMovement();
            HandleRotation();
        }

        public void FixedUpdateMovement()
        {
            // Not used in simplified version
        }

        public void StopMovement()
        {
            _moveInput = Vector2.zero;
        }
        #endregion

        #region Private Methods
        private void HandleMovement()
        {
            if (_moveInput.magnitude < 0.01f) return;
            
            // Convert 2D input to 3D movement (isometric uses XZ plane)
            Vector3 movement = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized * _moveSpeed * Time.deltaTime;
            transform.position += movement;
        }

        private void HandleRotation()
        {
            if (_moveInput.magnitude < 0.01f) return;
            
            // Calculate angle from input
            float angle = Mathf.Atan2(_moveInput.x, _moveInput.y) * Mathf.Rad2Deg;
            
            // Snap to nearest cardinal direction (North=0°, East=90°, South=180°, West=270°)
            float targetAngle = GetNearestCardinalAngle(angle);
            
            // Set rotation directly to cardinal direction
            transform.rotation = Quaternion.Euler(0f, targetAngle, 0f);
        }

        private float GetNearestCardinalAngle(float angle)
        {
            // Normalize angle to 0-360 range
            while (angle < 0f) angle += 360f;
            while (angle >= 360f) angle -= 360f;
            
            float nearestAngle = _cardinalAngles[0];
            float smallestDifference = Mathf.Abs(Mathf.DeltaAngle(angle, nearestAngle));
            
            for (int i = 1; i < _cardinalAngles.Length; i++)
            {
                float difference = Mathf.Abs(Mathf.DeltaAngle(angle, _cardinalAngles[i]));
                if (difference < smallestDifference)
                {
                    smallestDifference = difference;
                    nearestAngle = _cardinalAngles[i];
                }
            }
            
            return nearestAngle;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set movement speed
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = Mathf.Max(0f, speed);
        }
        #endregion
    }
}
