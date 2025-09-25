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
    public class IsometricCameraControl : BaseCameraComponent
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

        #region Isometric Camera Specific Fields
        // Zoom state
        private float _currentZoom = 10.0f;
        private float _targetZoom = 10.0f;
        private float _zoomVelocity = 0f;
        #endregion

        #region BaseCameraComponent Implementation
        protected override void InitializeCameraSpecific()
        {
            if (_cinemachineCamera == null)
            {
                Debug.LogError("[IsometricCameraControl] CinemachineCamera is required but not assigned.");
                return;
            }
            
            // Set up orthographic camera
            var camera = GameManager.GetService<IGameDataService>().GetMainCamera();
            if (camera != null)
            {
                camera.orthographic = true;
            }
            
            // Initialize zoom
            _currentZoom = (_minZoom + _maxZoom) / 2f;
            _targetZoom = _currentZoom;
            
            // Apply initial zoom
            if (camera != null)
            {
                camera.orthographicSize = _currentZoom;
            }
            
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
        }

        protected override void ProcessLookInput()
        {
            // Isometric cameras typically don't process look input for rotation
            // Look input is ignored for isometric view
        }

        protected override void UpdateCameraSpecific()
        {
            UpdateZoom();
        }
        #endregion
        
        #region Additional Isometric Camera Methods
        public Transform GetCameraTransform()
        {
            return _cinemachineCamera?.transform;
        }
        #endregion

        #region Private Methods
        private void UpdateZoom()
        {
            if (Mathf.Abs(_currentZoom - _targetZoom) > 0.01f)
            {
                _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, _zoomSmoothTime);
                
                // Apply zoom to camera (assuming orthographic camera)
                if (_cinemachineCamera != null)
                {
                    var camera = _cinemachineCamera.GetComponent<UnityEngine.Camera>();
                    if (camera != null && camera.orthographic)
                    {
                        camera.orthographicSize = _currentZoom;
                    }
                }
            }
        }

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
        #endregion
    }
}