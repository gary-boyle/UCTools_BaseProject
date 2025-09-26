using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Core;
using GameFramework.Config.ScriptableObjects;

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
        
        [Header("Distance Settings")]
        [SerializeField] private float _followDistance = 5.0f;
        
        
        #endregion

        #region Third Person Camera Specific Fields
        private InputSettings_SO _inputSettings;
        private CinemachineThirdPersonFollow _cinemachineThirdPersonFollow;

        // Distance state
        private float _currentDistance = 5.0f;
        private float _targetDistance = 5.0f;
        
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
            
            if (_cinemachineThirdPersonFollow == null && _cinemachineCamera != null)
            {
                _cinemachineThirdPersonFollow = _cinemachineCamera.gameObject.GetComponent<CinemachineThirdPersonFollow>();
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
            _cinemachineThirdPersonFollow.VerticalArmLength = _currentZoom;
        }

        protected override void CleanupCameraSpecific()
        {
            // Clear references
            _inputSettings = null;
        }

        protected override void ProcessLookInput()
        {
            // noop
        }

        protected override void UpdateCameraSpecific()
        {
            // noop
        }

        #endregion
        
        #region Private Methods
        private void ApplyInputSettings()
        {
            if (_inputSettings == null) return;
            
            _globalMouseSensitivity = _inputSettings.GetMouseSensitivity();
            _globalInvertYAxis = _inputSettings.GetInvertYAxis();
        }

        protected override void UpdateZoom()
        {
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, _zoomSmoothness * Time.deltaTime);
            
            if (!(Mathf.Abs(_currentZoom - _targetZoom) > 0.01f)) return;
                
            if (_cinemachineCamera == null) return;
            
            _cinemachineThirdPersonFollow.VerticalArmLength = _currentZoom;
        }
        #endregion
    }
}
