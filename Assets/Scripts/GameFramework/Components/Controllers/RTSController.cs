using UnityEngine;
using GameFramework.Components.Controllers.Camera;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.Input;
using GameFramework.Services.Interfaces;

namespace GameFramework.Components.Controllers
{
    /// <summary>
    /// Real-Time Strategy controller that manages camera movement and unit selection.
    /// Uses a single RTSCameraControl component for all camera functionality.
    /// Handles unit selection with mouse clicks and selection boxes.
    /// </summary>
    public class RTSController : BasePlayerController
    {
        #region Serialized Fields
        [Header("Camera Component")]
        [SerializeField] private RTSCameraControl _cameraControl;
        
        [Header("Camera Focus")]
        [SerializeField] private bool _enableCameraFocus = true;
        [SerializeField] private float _focusSpeed = 5.0f;
        #endregion

        #region Private Fields
        private UnityEngine.Camera _mainCamera;
        
        // Camera focus
        private bool _isFocusing = false;
        private Vector3 _focusTarget;
        private float _focusStartTime;
        
        // UI and visual feedback
        private Texture2D _selectionBoxTexture;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            _mainCamera = GameManager.GetService<IGameDataService>().GetMainCamera();

            // Override input context for RTS
            _requiredInputContext = InputContext.Mixed; // RTS needs both camera and UI input
            
            // Find camera control component if not assigned
            if (_cameraControl == null) _cameraControl = GetComponent<RTSCameraControl>();
            
            // Subscribe to UI events for selection
            if (_eventSystem != null)
            {
            }
        }

        protected override void Update()
        {
            base.Update();
            
            if (_isInitialized)
            {
                HandleCameraFocus();
            }
        }
        
        protected override void OnDestroy()
        {
            // Unsubscribe from UI events
            if (_eventSystem != null)
            {
            }
            
            base.OnDestroy();
        }
        #endregion

        #region Component Creation
        protected override void CreateComponents()
        {
        }
        
        #endregion

        #region Input Handling
        
        private void HandleCameraFocus()
        {
            if (!_enableCameraFocus || !_isFocusing || _cameraControl == null) return;
            
            float elapsedTime = Time.time - _focusStartTime;
            float t = elapsedTime * _focusSpeed;
            
            if (t >= 1.0f)
            {
                _cameraControl.FocusOnPosition(_focusTarget);
                _isFocusing = false;
            }
            else
            {
                // Smoothly interpolate camera position using the camera control component
                Vector3 currentPos = _cameraControl.GetCameraTransform().position;
                Vector3 targetPos = Vector3.Lerp(currentPos, _focusTarget, t);
                _cameraControl.FocusOnPosition(targetPos);
            }
        }
        #endregion
        
        #region Input Event Handlers - Override for RTS-specific behavior
        protected override void OnPlayerMoveInput(PlayerMoveInputEvent inputEvent)
        {
            // In RTS, move input controls camera movement (WASD)
            if (_cameraControl != null)
            {
                _cameraControl.HandleMoveInput(inputEvent);
            }
        }
        #endregion
        
        #region Debug
        protected override void OnDrawGizmos()
        {

            base.OnDrawGizmos();
            
            if (!_showDebugInfo) return;
            
            // Draw focus target
            if (_isFocusing)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_focusTarget, 1f);
            }
        }
        #endregion
    }
}
