using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;
using IInputService = GameFramework.Services.Interfaces.IInputService;

namespace GameFramework.Services
{
    /// <summary>
    /// Efficient event-driven Input service using Unity's generated InputSystem_Actions
    /// Only processes input when events actually occur, no polling overhead
    /// </summary>
    public class InputService : IInputService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private InputSystem_Actions _inputActions;
        
        // Only cache values that we actually need for legacy interface compatibility
        private Vector2 _lastMovement;
        private Vector2 _lastLook;
        private Vector2 _lastMousePosition;
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public InputService(IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[InputService] Initializing event-driven Input System...");
            
            // Create instance of generated input actions
            _inputActions = new InputSystem_Actions();
            
            // Subscribe to specific events we care about
            SubscribeToInputEvents();
            
            // Enable the input actions
            _inputActions.Enable();
            
            IsInitialized = true;
            Debug.Log("[InputService] Event-driven Input System initialized successfully");
            
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            Debug.Log("[InputService] Shutting down Input System...");
            
            if (_inputActions != null)
            {
                // Unsubscribe from events
                UnsubscribeFromInputEvents();
                
                // Disable and dispose
                _inputActions.Disable();
                _inputActions.Dispose();
                _inputActions = null;
            }
            
            IsInitialized = false;
        }
        
        private void SubscribeToInputEvents()
        {
            // Player Actions - Subscribe to specific events
            _inputActions.Player.Move.performed += OnMoveInput;
            _inputActions.Player.Look.performed += OnLookInput;
            _inputActions.Player.Attack.performed += OnAttackInput;
            _inputActions.Player.Interact.performed += OnInteractInput;
            _inputActions.Player.Crouch.performed += OnCrouchInput;
            _inputActions.Player.Jump.performed += OnJumpInput;
            _inputActions.Player.Previous.performed += OnPreviousInput;
            _inputActions.Player.Next.performed += OnNextInput;
            _inputActions.Player.Sprint.performed += OnSprintInput;
            _inputActions.Player.Pause.performed += OnPauseInput;

            // UI Actions
            _inputActions.UI.Navigate.performed += OnNavigateInput;
            _inputActions.UI.Submit.performed += OnSubmitInput;
            _inputActions.UI.Cancel.performed += OnCancelInput;
            _inputActions.UI.Point.performed += OnPointInput;
            _inputActions.UI.Click.performed += OnClickInput;
            //_inputActions.UI.Click.started += OnClickInput;
            //_inputActions.UI.Click.canceled += OnClickInput;
            _inputActions.UI.RightClick.performed += OnRightClickInput;
            _inputActions.UI.MiddleClick.performed += OnMiddleClickInput;
            _inputActions.UI.ScrollWheel.performed += OnScrollWheelInput;
            _inputActions.UI.TrackedDevicePosition.performed += OnTrackedDevicePositionInput;
            _inputActions.UI.TrackedDeviceOrientation.performed += OnTrackedDeviceOrientationInput;
        }
        
        private void UnsubscribeFromInputEvents()
        {
            if (_inputActions == null) return;
            
            // Player Actions
            _inputActions.Player.Move.performed -= OnMoveInput;
            _inputActions.Player.Look.performed -= OnLookInput;
            _inputActions.Player.Attack.performed -= OnAttackInput;
            _inputActions.Player.Interact.performed -= OnInteractInput;
            _inputActions.Player.Crouch.performed -= OnCrouchInput;
            _inputActions.Player.Jump.performed -= OnJumpInput;
            _inputActions.Player.Previous.performed -= OnPreviousInput;
            _inputActions.Player.Next.performed -= OnNextInput;
            _inputActions.Player.Sprint.performed -= OnSprintInput;
            _inputActions.Player.Pause.performed -= OnPauseInput;

            // UI Actions
            _inputActions.UI.Navigate.performed -= OnNavigateInput;
            _inputActions.UI.Submit.performed -= OnSubmitInput;
            _inputActions.UI.Cancel.performed -= OnCancelInput;
            _inputActions.UI.Point.performed -= OnPointInput;
            _inputActions.UI.Click.performed -= OnClickInput;
            //_inputActions.UI.Click.started -= OnClickInput;
            //_inputActions.UI.Click.canceled -= OnClickInput;
            _inputActions.UI.RightClick.performed -= OnRightClickInput;
            _inputActions.UI.MiddleClick.performed -= OnMiddleClickInput;
            _inputActions.UI.ScrollWheel.performed -= OnScrollWheelInput;
            _inputActions.UI.TrackedDevicePosition.performed -= OnTrackedDevicePositionInput;
            _inputActions.UI.TrackedDeviceOrientation.performed -= OnTrackedDeviceOrientationInput;
            
        }
        #region Player Input Event Handlers

        private void OnMoveInput(InputAction.CallbackContext context)
        {
            _lastMovement = context.ReadValue<Vector2>();
            Debug.Log($"[InputService] Move: {_lastMovement} - Phase: {context.phase}");
            
            _eventSystem.Publish(new PlayerMoveInputEvent(_lastMovement, context.phase));
        }

        private void OnLookInput(InputAction.CallbackContext context)
        {
            _lastLook = context.ReadValue<Vector2>();
            // Don't log look input as it's very frequent
            
            _eventSystem.Publish(new PlayerLookInputEvent(_lastLook, context.phase));
        }

        private void OnAttackInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Attack - Phase: {context.phase}");
            
            _eventSystem.Publish(new PlayerAttackInputEvent(context.phase));
        }

        private void OnInteractInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Interact - Phase: {context.phase}");
            
            _eventSystem.Publish(new PlayerInteractInputEvent(context.phase));
        }

        private void OnCrouchInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Crouch - Phase: {context.phase}");
            
            _eventSystem.Publish(new PlayerCrouchInputEvent(context.phase));
        }

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Jump - Phase: {context.phase}");
            
            _eventSystem.Publish(new PlayerJumpInputEvent());
        }

        private void OnPreviousInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Previous - Phase: {context.phase}");
            
            _eventSystem.Publish(new PlayerPreviousInputEvent());
        }

        private void OnNextInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Next - Phase: {context.phase}");
            
            _eventSystem.Publish(new PlayerNextInputEvent());
        }

        private void OnSprintInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Sprint - Phase: {context.phase}");
            
            _eventSystem.Publish(new PlayerSprintInputEvent(context.phase));
        }

        private void OnPauseInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Pause - Phase: {context.phase}");
    
            _eventSystem.Publish(new PlayerPauseInputEvent(context.phase));
        }
        
        #endregion

        #region UI Input Event Handlers

        private void OnNavigateInput(InputAction.CallbackContext context)
        {
            var navigationValue = context.ReadValue<Vector2>();
            Debug.Log($"[InputService] UI Navigate: {navigationValue}");
            
            _eventSystem.Publish(new UINavigateInputEvent(navigationValue));
        }

        private void OnSubmitInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] UI Submit");
            
            _eventSystem.Publish(new UISubmitInputEvent());
        }

        private void OnCancelInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] UI Cancel");
            
            _eventSystem.Publish(new UICancelInputEvent());
        }

        private void OnPointInput(InputAction.CallbackContext context)
        {
            _lastMousePosition = context.ReadValue<Vector2>();
            // Don't log mouse position as it's very frequent
            
            _eventSystem.Publish(new UIPointInputEvent(_lastMousePosition));
        }

        private void OnClickInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] UI Click - Phase: {context.phase}");
            
            _eventSystem.Publish(new UIClickInputEvent(context.phase));
        }

        private void OnRightClickInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] UI Right Click");
            
            _eventSystem.Publish(new UIRightClickInputEvent());
        }

        private void OnMiddleClickInput(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] UI Middle Click");
            
            _eventSystem.Publish(new UIMiddleClickInputEvent());
        }

        private void OnScrollWheelInput(InputAction.CallbackContext context)
        {
            var scrollValue = context.ReadValue<Vector2>();
            Debug.Log($"[InputService] UI Scroll Wheel: {scrollValue}");
            
            _eventSystem.Publish(new UIScrollWheelInputEvent(scrollValue));
        }

        private void OnTrackedDevicePositionInput(InputAction.CallbackContext context)
        {
            var position = context.ReadValue<Vector3>();
            // Don't log as it's very frequent
            
            _eventSystem.Publish(new UITrackedDevicePositionInputEvent(position));
        }

        private void OnTrackedDeviceOrientationInput(InputAction.CallbackContext context)
        {
            var orientation = context.ReadValue<Quaternion>();
            // Don't log as it's very frequent
            
            _eventSystem.Publish(new UITrackedDeviceOrientationInputEvent(orientation));
        }

        #endregion
        #region New Input System Methods
        
        /// <summary>
        /// Get the raw InputActions instance for advanced usage
        /// </summary>
        public InputSystem_Actions GetInputActions() => _inputActions;
        
        /// <summary>
        /// Get current movement input (updated only when changed)
        /// </summary>
        public Vector2 GetMovementInput() => _lastMovement;
        
        /// <summary>
        /// Get current look input (updated only when changed)
        /// </summary>
        public Vector2 GetLookInput() => _lastLook;
        
        /// <summary>
        /// Enable specific action map
        /// </summary>
        public void EnableActionMap(string mapName)
        {
            if (_inputActions == null) return;
            
            switch (mapName.ToLower())
            {
                case "player":
                    _inputActions.Player.Enable();
                    Debug.Log("[InputService] Enabled Player action map");
                    break;
                case "ui":
                    _inputActions.UI.Enable();
                    Debug.Log("[InputService] Enabled UI action map");
                    break;
                default:
                    Debug.LogWarning($"[InputService] Unknown action map: {mapName}");
                    break;
            }
        }
        
        /// <summary>
        /// Disable specific action map
        /// </summary>
        public void DisableActionMap(string mapName)
        {
            if (_inputActions == null) return;
            
            switch (mapName.ToLower())
            {
                case "player":
                    _inputActions.Player.Disable();
                    Debug.Log("[InputService] Disabled Player action map");
                    break;
                case "ui":
                    _inputActions.UI.Disable();
                    Debug.Log("[InputService] Disabled UI action map");
                    break;
                default:
                    Debug.LogWarning($"[InputService] Unknown action map: {mapName}");
                    break;
            }
        }
        
        #endregion

        public void Update()
        {
        }
    }
}
