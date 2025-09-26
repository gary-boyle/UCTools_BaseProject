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
    /// First-person camera control using Cinemachine 3.1+.
    /// Provides direct mouse look with smooth rotation and sensitivity controls.
    /// </summary>
    public class FirstPersonCameraControl : BaseCameraComponent
    {
        #region Serialized Fields
        [Header("Player and camera Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _followTarget;
        [SerializeField] private Transform _playerTransform;

        [Header("Look Settings")]
        [SerializeField] private float _mouseSensitivityMultiplier = 1.0f;
        [SerializeField] private float _minVerticalAngle = -80f;
        [SerializeField] private float _maxVerticalAngle = 80f;
        [SerializeField] private bool _invertYAxis = false;
        
        #endregion

        #region First Person Camera Specific Fields
        private InputSettings_SO _inputSettings;
        private CinemachinePanTilt _cinemachinePanTilt;

        // Look state
        private float _currentPitch = 0f;
        private float _currentYaw = 0f;
        private Vector2 smoothedMouseDelta;
        
        // Input processing
        private float _globalMouseSensitivity = 1.0f;
        private bool _globalInvertYAxis = false;
        
        // Cursor state management
        private bool _wasLockedBeforePause = true;

        #endregion

        #region First Person Camera Specific Properties
        public float MouseSensitivityMultiplier 
        { 
            get => _mouseSensitivityMultiplier; 
            set => _mouseSensitivityMultiplier = Mathf.Clamp(value, 0.001f, 5.0f); 
        }
        
        public float EffectiveMouseSensitivity => _globalMouseSensitivity * _mouseSensitivityMultiplier;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            // Get input settings
            _inputSettings = SettingsRegistry.Get<InputSettings_SO>();
            if (_inputSettings != null)
            {
                ApplyInputSettings();
            }
        }
        #endregion

        #region BaseCameraComponent Implementation
        protected override void InitializeCameraSpecific()
        {
            if (_cinemachineCamera == null)
            {
                Debug.LogError("[FirstPersonCameraControl] CinemachineCamera is required but not assigned.");
                return;
            }
            
            if (_playerTransform != null)
            {
                // Initialize yaw from player's current rotation
                _currentYaw = _playerTransform.eulerAngles.y;
            }

            if (_cinemachinePanTilt == null)
            {
                _cinemachinePanTilt = _cinemachineCamera.GetComponent<CinemachinePanTilt>();
            }
        }

        protected override void CleanupCameraSpecific()
        {
            // Clear references
            _inputSettings = null;
        }

        
        protected override void ProcessLookInput()
        {
            float mouseXRotation = _lookInput.x * EffectiveMouseSensitivity;
            transform.Rotate(0, mouseXRotation, 0);
            
            _currentPitch -= _lookInput.y * EffectiveMouseSensitivity;
            _currentPitch = Mathf.Clamp(_currentPitch, _minVerticalAngle, _maxVerticalAngle);
            _cinemachinePanTilt.TiltAxis.Value = _currentPitch;
        }

        protected override void UpdateCameraSpecific()
        {
            // noop
        }

        protected override void UpdateZoom()
        {
            // noop
        }

        private void ApplyRotation()
        {
            // Apply horizontal rotation (yaw) to the player character
            if (_playerTransform != null)
            {
                Quaternion playerRotation = Quaternion.Euler(0f, _currentYaw, 0f);
                _playerTransform.rotation = playerRotation;
            }
            
            _cinemachinePanTilt.TiltAxis.Value = _currentPitch;
        }
        

        #endregion
        
        #region Private Methods
        private void ApplyInputSettings()
        {
            if (_inputSettings == null) return;
            
            _globalMouseSensitivity = _inputSettings.GetMouseSensitivity();
            _globalInvertYAxis = _inputSettings.GetInvertYAxis();
        }
        #endregion
    }
}