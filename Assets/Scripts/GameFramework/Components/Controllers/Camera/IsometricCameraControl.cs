using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;

namespace GameFramework.Components.Controllers.Camera
{
    /// <summary>
    /// Simple isometric camera control using Cinemachine 3.1+.
    /// Provides only zoom functionality for isometric/top-down games.
    /// </summary>
    public class IsometricCameraControl : MonoBehaviour, ICameraControl
    {
        #region Serialized Fields
        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        
        [Header("Zoom Settings")]
        [SerializeField] private float _zoomSpeed = 2.0f;
        [SerializeField] private float _minZoom = 5.0f;
        [SerializeField] private float _maxZoom = 20.0f;
        [SerializeField] private float _zoomSmoothTime = 0.2f;
        #endregion

        #region Private Fields
        private IPauseService _pauseService;
        private IEventSystem _eventSystem;
        
        // Zoom state
        private float _currentZoom = 10.0f;
        private float _targetZoom = 10.0f;
        private float _zoomVelocity = 0f;
        
        // Component state
        private bool _isInitialized = false;
        private bool _inputEnabled = true;
        #endregion

        #region Public Properties
        public bool IsPaused => _pauseService?.IsPaused ?? false;

        public float MouseSensitivityMultiplier { get; set; }

        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _pauseService = GameManager.GetService<IPauseService>();
            _eventSystem = GameManager.GetService<IEventSystem>();
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
                Debug.LogError("[IsometricCameraControl] CinemachineCamera is required but not assigned.");
                return;
            }
            
            // Initialize zoom
            _currentZoom = _minZoom + (_maxZoom - _minZoom) * 0.5f;
            _targetZoom = _currentZoom;
            
            // Set orthographic projection
            GameManager.GetService<IGameDataService>().SetCameraOrthographic(true);
            _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
            
            _isInitialized = true;
            
            // Subscribe to scroll wheel events for zoom
            if (_eventSystem != null)
            {
                _eventSystem.Subscribe<UIScrollWheelInputEvent>(OnScrollWheel);
            }
        }

        public void Cleanup()
        {
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<UIScrollWheelInputEvent>(OnScrollWheel);
            }

            _pauseService = null;
            _eventSystem = null;
            _isInitialized = false;
        }

        public void HandleLookInput(PlayerLookInputEvent inputEvent)
        {
            // No look input handling for simple zoom-only camera
        }

        public void UpdateCamera()
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            UpdateZoom();
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }
        #endregion

        #region Private Methods
        private void OnScrollWheel(UIScrollWheelInputEvent scrollEvent)
        {
            if (!_isInitialized || !_inputEnabled || IsPaused) return;
            
            float scrollInput = scrollEvent.ScrollDelta.y;
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                _targetZoom -= scrollInput * _zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            }
        }

        private void UpdateZoom()
        {
            // Apply smooth zoom
            _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, _zoomSmoothTime);
            
            // Update Cinemachine orthographic size
            _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
        }
        #endregion
    }
}
