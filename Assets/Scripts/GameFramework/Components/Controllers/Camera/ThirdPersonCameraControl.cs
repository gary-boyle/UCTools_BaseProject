using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.Config.ScriptableObjects;
using GameFramework.EventSystem.Interfaces;

namespace GameFramework.Components.Controllers.Camera
{
    /// <summary>
    /// Third-person camera control using Cinemachine 3.1+.
    /// Provides orbiting camera with follow behavior and collision detection.
    /// </summary>
    public class ThirdPersonCameraControl : BaseCameraComponent
    {
        #region Serialized Fields
        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Transform _lookAtTarget;
        
        [Header("Orbit Settings")]
        [SerializeField] private float _mouseSensitivityMultiplier = 1.0f;
        [SerializeField] private float _orbitSpeed = 2.0f;
        [SerializeField] private float _minVerticalAngle = -30f;
        [SerializeField] private float _maxVerticalAngle = 60f;
        [SerializeField] private bool _invertYAxis = false;
        
        [Header("Distance Settings")]
        [SerializeField] private float _followDistance = 5.0f;
        [SerializeField] private float _minDistance = 2.0f;
        [SerializeField] private float _maxDistance = 10.0f;
        [SerializeField] private float _zoomSpeed = 2.0f;
        [SerializeField] private float _zoomSmoothTime = 0.2f;
        
        #endregion

        #region Third Person Camera Specific Fields
        private InputSettings_SO _inputSettings;
        
        // Look state  
        private float _currentOrbitY = 0f;
        
        // Distance state
        private float _currentDistance = 5.0f;
        private float _targetDistance = 5.0f;
        private float _zoomVelocity = 0f;
        
        // Input processing
        private float _globalMouseSensitivity = 1.0f;
        private bool _globalInvertYAxis = false;
        
        // Cursor state management
        private bool _wasLockedBeforePause = true;
        #endregion

        #region Third Person Camera Specific Properties
        public float MouseSensitivityMultiplier 
        { 
            get => _mouseSensitivityMultiplier; 
            set => _mouseSensitivityMultiplier = Mathf.Clamp(value, 0.001f, 2.0f); 
        }
        public float EffectiveMouseSensitivity => _globalMouseSensitivity * _mouseSensitivityMultiplier;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake(); // Get services from base class
            
            // Get input settings
            _inputSettings = SettingsRegistry.Get<InputSettings_SO>();
            if (_inputSettings != null)
            {
                ApplyInputSettings();
            }
            
            // Set lookAt target to follow target if not specified
            if (_lookAtTarget == null)
            {
                _lookAtTarget = _followTarget;
            }
        }
        #endregion

        #region BaseCameraComponent Implementation
        protected override void InitializeCameraSpecific()
        {
            if (_cinemachineCamera == null)
            {
                Debug.LogError("[ThirdPersonCameraControl] CinemachineCamera is required but not assigned.");
                return;
            }
            
            // Initialize distance
            _currentDistance = _followDistance;
            _targetDistance = _followDistance;
            
            // Subscribe to scroll wheel events for zoom
            if (_eventSystem != null)
            {
                _eventSystem.Subscribe<UIScrollWheelInputEvent>(OnScrollWheel);
            }
        }

        protected override void CleanupCameraSpecific()
        {
            // Unsubscribe from events
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<UIScrollWheelInputEvent>(OnScrollWheel);
            }

            // Clear references
            _inputSettings = null;
        }

        protected override void ProcessLookInput()
        {
            if (_lookInput.magnitude < 0.01f) return;
            
            // Apply sensitivity - only process vertical input for camera orbit
            float verticalInput = _lookInput.y * EffectiveMouseSensitivity * _orbitSpeed;
            
            // Apply Y-axis inversion for vertical input
            if (_globalInvertYAxis || _invertYAxis)
            {
                verticalInput *= -1f;
            }
            
            // Update vertical orbit only (horizontal rotation is now handled by movement component)
            _currentOrbitY -= verticalInput;
            
            // Clamp vertical orbit
            _currentOrbitY = Mathf.Clamp(_currentOrbitY, _minVerticalAngle, _maxVerticalAngle);
        }

        protected override void UpdateCameraSpecific()
        {
            UpdateZoom();
            // Input processing is now handled immediately in HandleLookInput
        }
        #endregion
        
        #region Additional Third Person Camera Methods
        public void SetTarget(Transform target)
        {
            _followTarget = target;
            _lookAtTarget = target; // Also use as look target
            
            if (_cinemachineCamera != null && target != null)
            {
                _cinemachineCamera.Follow = target;
                _cinemachineCamera.LookAt = target;
            }
        }

        public Transform GetCameraTransform()
        {
            return _cinemachineCamera?.transform;
        }
        #endregion

        #region Private Methods
        private void ApplyInputSettings()
        {
            if (_inputSettings == null) return;
            
            _globalMouseSensitivity = _inputSettings.GetMouseSensitivity();
            _globalInvertYAxis = _inputSettings.GetInvertYAxis();
        }

        private void UpdateZoom()
        {
            if (Mathf.Abs(_currentDistance - _targetDistance) > 0.01f)
            {
                _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _zoomVelocity, _zoomSmoothTime);
                
                // Apply distance to Cinemachine camera
                if (_cinemachineCamera != null)
                {
                    // For Cinemachine 3.1+, we need to handle distance via the camera's position relative to target
                    // This assumes the camera is set up with proper follow and look at targets
                    // var cinemachineOrbitalTransposer = _cinemachineCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
                    // if (cinemachineOrbitalTransposer != null)
                    // {
                    //     cinemachineOrbitalTransposer.FollowOffset = new Vector3(0, 0, -_currentDistance);
                    // }
                    // else
                    // {
                        // Fallback: directly manipulate camera distance
                        if (_followTarget != null)
                        {
                            Vector3 direction = (_cinemachineCamera.transform.position - _followTarget.position).normalized;
                            _cinemachineCamera.transform.position = _followTarget.position + direction * _currentDistance;
                        }
                    // }
                }
            }
        }

        private void OnScrollWheel(UIScrollWheelInputEvent scrollEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            float scrollInput = scrollEvent.ScrollDelta.y;
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                _targetDistance -= scrollInput * _zoomSpeed;
                _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
            }
        }
        #endregion
    }
}
