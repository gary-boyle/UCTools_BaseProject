using UnityEngine;
using GameFramework.Components.Controllers.Camera;
using GameFramework.Components.Controllers.Enum;
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
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            // Override input context for RTS
            _requiredInputContext = InputContext.Mixed; // RTS needs both camera and UI input
        }

        #endregion

        #region Component Management
        protected override void CreateComponents()
        {
            // Find and assign camera component (RTS uses RTSCameraControl instead of standard movement/camera)
            var cameraComponent = GetComponent<RTSCameraControl>();
            if (cameraComponent == null)
            {
                Debug.LogError($"[{GetType().Name}] RTSCameraControl component not found on {gameObject.name}");
                return;
            }

            // RTS controller uses RTSCameraControl instead of standard movement/camera components
            _cameraComponent = cameraComponent;
        }
        
        protected override PlayerPrefabType GetControllerType()
        {
            return PlayerPrefabType.RTS;
        }
        #endregion

        
        #region Input Event Handlers - Override for RTS-specific behavior
        protected override void OnPlayerMoveInput(PlayerMoveInputEvent inputEvent)
        {
            // In RTS, move input controls camera movement (WASD)
            if (_cameraComponent is RTSCameraControl rtsCameraControl)
            {
                rtsCameraControl.HandleMoveInput(inputEvent);
            }
        }
        
        /// <summary>
        /// Override interaction input for RTS - trigger interaction via mouse click
        /// </summary>
        protected override void OnPlayerInteractInput(PlayerInteractInputEvent inputEvent)
        {
            if (!_isInitialized || !_isEnabled) return;
            
            // For RTS, we trigger interaction on mouse click (left click is typically the interact input)
            if (inputEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                _interactionDetector?.TriggerInteraction();
            }
        }
        #endregion
        
        #region Debug
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            if (!_showDebugInfo) return;
            
        }
        #endregion
    }
}