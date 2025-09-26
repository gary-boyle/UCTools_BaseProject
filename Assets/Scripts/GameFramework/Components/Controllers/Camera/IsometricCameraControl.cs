using UnityEngine;
using Unity.Cinemachine;

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
            if (_mainCamera != null)
            {
                _mainCamera.orthographic = true;
            }
            
            // Initialize zoom
            _currentZoom = (_minZoom + _maxZoom) / 2f;
            _targetZoom = _currentZoom;
            
            // Apply initial zoom
            if (_mainCamera != null)
            {
                _mainCamera.orthographicSize = _currentZoom;
            }
        }

        protected override void CleanupCameraSpecific()
        {
            // noop
        }

        protected override void ProcessLookInput()
        {
            // noop
        }


        protected override void UpdateCameraSpecific()
        {
            UpdateZoom();
        }
        
        #endregion
        
        #region Private Methods
        protected override void UpdateZoom()
        {
            if (!(Mathf.Abs(_currentZoom - _targetZoom) > 0.01f)) return;
            
            _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, _zoomSmoothTime);
                
            // Apply zoom to camera (assuming orthographic camera)
            if (_cinemachineCamera == null) return;
            if (_mainCamera.orthographic)
            {
                _cinemachineCamera.Lens.OrthographicSize = _currentZoom;
            }
        }

        #endregion
    }
}