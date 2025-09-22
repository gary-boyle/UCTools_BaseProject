using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.Config.ScriptableObjects;

namespace GameFramework.Components.Controllers.Camera
{
    /// <summary>
    /// First-person camera control using Cinemachine 3.1+.
    /// Provides direct mouse look with smooth rotation and sensitivity controls.
    /// </summary>
    public class FirstPersonCameraControl : MonoBehaviour, ICameraControl
    {
        #region Serialized Fields
        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] private Transform _followTarget;
        
        [Header("Look Settings")]
        [SerializeField] private float _mouseSensitivityMultiplier = 0.01f;
        [SerializeField] private float _minVerticalAngle = -80f;
        [SerializeField] private float _maxVerticalAngle = 80f;
        [SerializeField] private bool _invertYAxis = false;
        
        [Header("Cursor Settings")]
        [SerializeField] private bool _lockCursor = true;
        [SerializeField] private bool _hideCursor = true;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        #endregion

        #region Private Fields
        private IPauseService _pauseService;
        private InputSettings_SO _inputSettings;
        
        // Player and camera targets
        private Transform _playerTransform; // The player character (parent of camera mount)
        
        // Look state
        private Vector2 _lookInput = Vector2.zero;
        private float _currentPitch = 0f;
        private float _currentYaw = 0f;
        
        // Input processing
        private float _globalMouseSensitivity = 1.0f;
        private bool _globalInvertYAxis = false;
        private bool _inputEnabled = true;
        
        // Cursor state management
        private bool _wasLockedBeforePause = true;
        
        // Component state
        private bool _isInitialized = false;
        #endregion

        #region Public Properties
        public bool IsPaused => _pauseService?.IsPaused ?? false;
        public Vector2 CurrentLookInput => _lookInput;
        public float MouseSensitivityMultiplier 
        { 
            get => _mouseSensitivityMultiplier; 
            set => _mouseSensitivityMultiplier = value; 
        }
        public float EffectiveMouseSensitivity => _globalMouseSensitivity * _mouseSensitivityMultiplier;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Get services
            _pauseService = GameManager.GetService<IPauseService>();
            
            // Get input settings
            _inputSettings = SettingsRegistry.Get<InputSettings_SO>();
            if (_inputSettings != null)
            {
                ApplyInputSettings();
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            UpdateCamera();
        }
        #endregion

        #region ICameraControl Implementation
        public void Initialize()
        {
            if (_isInitialized) return;
            
            if (_cinemachineCamera == null)
            {
                Debug.LogError("[FirstPersonCameraControl] CinemachineCamera is required but not assigned.");
                return;
            }
            
            // Setup Cinemachine camera for first-person
            //SetupCinemachineCamera();
            
            // Initialize cursor state
            if (_lockCursor)
            {
                SetCursorLocked(true);
            }
            
            // Initialize rotation from current transforms
            if (_playerTransform != null)
            {
                _currentYaw = _playerTransform.eulerAngles.y;
            }
            
            if (_followTarget != null)
            {
                Vector3 localEulerAngles = _followTarget.localEulerAngles;
                _currentPitch = localEulerAngles.x;
                
                // Handle wrap-around for pitch
                if (_currentPitch > 180f)
                    _currentPitch -= 360f;
            }
            
            _isInitialized = true;
            
            if (_showDebugInfo)
                Debug.Log("[FirstPersonCameraControl] Initialized successfully");
        }

        public void Cleanup()
        {
            // Ensure cursor is unlocked
            SetCursorLocked(false);
            
            // Clear references
            _pauseService = null;
            _inputSettings = null;
            
            _isInitialized = false;
            
            if (_showDebugInfo)
                Debug.Log("[FirstPersonCameraControl] Cleaned up");
        }

        public void HandleLookInput(PlayerLookInputEvent inputEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            _lookInput = inputEvent.LookDelta;
        }

        public void UpdateCamera()
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            ProcessLookInput();
            ApplyRotation();
            
            // Reset input after processing to prevent continuous rotation
            _lookInput = Vector2.zero;
        }

        public void SetTarget(Transform target)
        {
            _followTarget = target;
            
            // If the target is a camera mount, get the player transform (its parent)
            if (target != null && target.name.Contains("CameraMount") && target.parent != null)
            {
                _playerTransform = target.parent;
            }
            else if (target != null)
            {
                // If no camera mount pattern, use the target itself as the player
                _playerTransform = target;
            }
            
            if (_cinemachineCamera != null && target != null)
            {
                _cinemachineCamera.Follow = target;
                _cinemachineCamera.LookAt = target;
            }
            
            if (_showDebugInfo && _playerTransform != null)
            {
                Debug.Log($"[FirstPersonCameraControl] Set player transform to: {_playerTransform.name}");
            }
        }

        public Transform GetCameraTransform()
        {
            return _cinemachineCamera.transform;
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                _lookInput = Vector2.zero;
            }
        }
        #endregion

        #region Private Methods

        private void ApplyInputSettings()
        {
            if (_inputSettings == null) return;
            
            _globalMouseSensitivity = _inputSettings.GetMouseSensitivity();
            _globalInvertYAxis = _inputSettings.GetInvertYAxis();
            
            if (_showDebugInfo)
            {
                Debug.Log($"[FirstPersonCameraControl] Applied Input Settings - Global Sensitivity: {_globalMouseSensitivity}, Invert Y: {_globalInvertYAxis}");
            }
        }

        private void ProcessLookInput()
        {
            if (_lookInput.magnitude < 0.01f) return;
            
            // Apply sensitivity
            Vector2 processedInput = _lookInput * EffectiveMouseSensitivity;
            
            // Apply Y-axis inversion
            if (_globalInvertYAxis || _invertYAxis)
            {
                processedInput.y *= -1f;
            }
            
            // Update rotation values
            _currentYaw += processedInput.x;
            _currentPitch -= processedInput.y;
            
            // Clamp vertical rotation
            _currentPitch = Mathf.Clamp(_currentPitch, _minVerticalAngle, _maxVerticalAngle);
        }

        private void ApplyRotation()
        {
            // Apply horizontal rotation (yaw) to the player character
            if (_playerTransform != null)
            {
                Quaternion playerRotation = Quaternion.Euler(0f, _currentYaw, 0f);
                _playerTransform.rotation = playerRotation;
            }
            
            // Apply vertical rotation (pitch) to the camera mount
            if (_followTarget != null)
            {
                Quaternion cameraRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
                _followTarget.localRotation = cameraRotation;
            }
        }

        private void SetCursorLocked(bool locked)
        {
            if (locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                if (_hideCursor)
                    Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void OnGamePaused()
        {
            if (!_isInitialized) return;
            
            // Clear any pending input
            _lookInput = Vector2.zero;
            
            // Store cursor state and unlock for menu navigation
            _wasLockedBeforePause = Cursor.lockState == CursorLockMode.Locked;
            SetCursorLocked(false);
            
            if (_showDebugInfo)
                Debug.Log("[FirstPersonCameraControl] Game paused - cursor unlocked");
        }

        public void OnGameResumed()
        {
            if (!_isInitialized) return;
            
            // Restore cursor state if it was locked before pause
            if (_wasLockedBeforePause && _lockCursor)
            {
                SetCursorLocked(true);
            }
            
            if (_showDebugInfo)
                Debug.Log("[FirstPersonCameraControl] Game resumed - cursor state restored");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the vertical look angle limits
        /// </summary>
        public void SetVerticalAngleLimits(float minAngle, float maxAngle)
        {
            _minVerticalAngle = minAngle;
            _maxVerticalAngle = maxAngle;
            
            // Re-clamp current pitch
            _currentPitch = Mathf.Clamp(_currentPitch, _minVerticalAngle, _maxVerticalAngle);
        }
        
        /// <summary>
        /// Set cursor lock and visibility settings
        /// </summary>
        public void SetCursorSettings(bool lockCursor, bool hideCursor)
        {
            _lockCursor = lockCursor;
            _hideCursor = hideCursor;
            
            if (!IsPaused)
            {
                SetCursorLocked(_lockCursor);
            }
        }
        
        /// <summary>
        /// Reset vertical rotation to center
        /// </summary>
        public void ResetVerticalRotation()
        {
            _currentPitch = 0f;
        }
        
        /// <summary>
        /// Get the current look rotation in degrees
        /// </summary>
        public Vector2 GetCurrentRotation()
        {
            return new Vector2(_currentPitch, _currentYaw);
        }
        #endregion
    }
}
