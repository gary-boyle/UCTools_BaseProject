using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.StateMachine.Enum;
using UnityEngine;
using UnityEngine.InputSystem;
using IInputService = GameFramework.Services.Interfaces.IInputService;

namespace GameFramework.Services
{
    /// <summary>
    /// Enhanced event-driven Input service with context-based input map management
    /// Automatically enables/disables appropriate input maps based on game state
    /// </summary>
    public class InputService : IInputService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private InputSystem_Actions _inputActions;
        private InputContext _currentContext = InputContext.None;
        private bool _consoleInputEnabled = false;
        
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
    
            // ALWAYS enable console toggle - it should work in any context
            _inputActions.Console.ToggleConsole.Enable();
            Debug.Log("[InputService] Console toggle enabled");
    
            // Test if the console action is properly bound
            Debug.Log($"[InputService] Console toggle action: {_inputActions.Console.ToggleConsole.name}");
            Debug.Log($"[InputService] Console toggle bindings: {string.Join(", ", _inputActions.Console.ToggleConsole.bindings)}");
    
            // Subscribe to game state changes to automatically manage input contexts
            _eventSystem.Subscribe<GameStateChangeEvent>(OnGameStateChanged);
    
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
                _eventSystem.Unsubscribe<GameStateChangeEvent>(OnGameStateChanged);
                
                // Disable and dispose
                _inputActions.Disable();
                _inputActions.Dispose();
                _inputActions = null;
            }
            
            IsInitialized = false;
        }

        public void Update()
        {
            // This method is required by the interface but not used in event-driven input
        }

        /// <summary>
        /// Set the input context based on game state
        /// </summary>
        public void SetInputContext(InputContext context)
        {
            if (_currentContext == context) return;
            
            Debug.Log($"[InputService] Switching input context from {_currentContext} to {context}");
            
            // Disable current context
            DisableCurrentContext();
            
            // Enable new context
            _currentContext = context;
            EnableCurrentContext();
        }

        /// <summary>
        /// Set input context for a specific game state
        /// </summary>
        public void SetInputContextForState(GameStateType stateType)
        {
            var context = GetInputContextForState(stateType);
            SetInputContext(context);
        }

        /// <summary>
        /// Set console input enabled and manage UI input conflicts
        /// </summary>
        public void SetConsoleInputEnabled(bool enabled)
        {
            if (_consoleInputEnabled == enabled) return;

            _consoleInputEnabled = enabled;

            if (enabled)
            {
                // Enable console input actions
                _inputActions.Console.Submit.Enable();
                _inputActions.Console.TabComplete.Enable();
                _inputActions.Console.HistoryUp.Enable();
                _inputActions.Console.HistoryDown.Enable();
        
                // IMPORTANT: Disable UI actions that might interfere with text input
                // Keep navigation but disable submit/cancel which might conflict
                _inputActions.UI.Submit.Disable();
                _inputActions.UI.Cancel.Disable();
        
                Debug.Log("[InputService] Console input actions enabled, UI Submit/Cancel disabled");
            }
            else
            {
                // Disable console input actions
                _inputActions.Console.Submit.Disable();
                _inputActions.Console.TabComplete.Disable();
                _inputActions.Console.HistoryUp.Disable();
                _inputActions.Console.HistoryDown.Disable();
        
                // Re-enable UI actions based on current context
                if (_currentContext == InputContext.UI || _currentContext == InputContext.Mixed)
                {
                    _inputActions.UI.Submit.Enable();
                    _inputActions.UI.Cancel.Enable();
                }
        
                Debug.Log("[InputService] Console input actions disabled, UI actions restored");
            }
        }

        /// <summary>
        /// Get the appropriate input context for a game state
        /// </summary>
        private InputContext GetInputContextForState(GameStateType stateType)
        {
            return stateType switch
            {
                GameStateType.Bootstrap => InputContext.None,
                GameStateType.Splash => InputContext.UI,      // Allow UI input to skip splash
                GameStateType.MainMenu => InputContext.UI,
                GameStateType.Loading => InputContext.UI,     // Allow UI input to cancel loading
                GameStateType.NewGame => InputContext.UI,
                GameStateType.Options => InputContext.UI,
                GameStateType.Credits => InputContext.UI,
                GameStateType.GameOver => InputContext.UI,
                GameStateType.Victory => InputContext.UI,
                GameStateType.Playing => InputContext.Player,
                GameStateType.Paused => InputContext.Mixed,   // Need both UI for menus and some player input
                GameStateType.Quit => InputContext.None,
                _ => InputContext.UI
            };
        }

        /// <summary>
        /// Handle game state changes to automatically switch input contexts
        /// </summary>
        private void OnGameStateChanged(GameStateChangeEvent evt)
        {
            SetInputContextForState(evt.NewState);
        }

        /// <summary>
        /// Disable the current input context
        /// </summary>
        private void DisableCurrentContext()
        {
            switch (_currentContext)
            {
                case InputContext.UI:
                    _inputActions.UI.Disable();
                    break;
                case InputContext.Player:
                    _inputActions.Player.Disable();
                    break;
                case InputContext.Mixed:
                    _inputActions.UI.Disable();
                    _inputActions.Player.Disable();
                    break;
                case InputContext.None:
                default:
                    // Nothing to disable
                    break;
            }
        }

        /// <summary>
        /// Enable the current input context
        /// </summary>
        private void EnableCurrentContext()
        {
            switch (_currentContext)
            {
                case InputContext.UI:
                    _inputActions.UI.Enable();
                    Debug.Log("[InputService] UI input context enabled");
                    break;
                case InputContext.Player:
                    _inputActions.Player.Enable();
                    Debug.Log("[InputService] Player input context enabled");
                    break;
                case InputContext.Mixed:
                    _inputActions.UI.Enable();
                    _inputActions.Player.Enable();
                    Debug.Log("[InputService] Mixed input context enabled");
                    break;
                case InputContext.None:
                default:
                    Debug.Log("[InputService] No input context enabled");
                    break;
            }
        }

        // [Rest of your existing methods remain the same...]

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
            _inputActions.UI.RightClick.performed += OnRightClickInput;
            _inputActions.UI.MiddleClick.performed += OnMiddleClickInput;
            _inputActions.UI.ScrollWheel.performed += OnScrollWheelInput;
            _inputActions.UI.TrackedDevicePosition.performed += OnTrackedDevicePositionInput;
            _inputActions.UI.TrackedDeviceOrientation.performed += OnTrackedDeviceOrientationInput;
            
            // Console Actions
            _inputActions.Console.ToggleConsole.performed += OnConsoleToggled;
            _inputActions.Console.Submit.performed += OnConsoleSubmit;
            _inputActions.Console.TabComplete.performed += OnConsoleTabbed;
            _inputActions.Console.HistoryUp.performed += OnConsoleHistoryUp;
            _inputActions.Console.HistoryDown.performed += OnConsoleHistoryDown;
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
            _inputActions.UI.RightClick.performed -= OnRightClickInput;
            _inputActions.UI.MiddleClick.performed -= OnMiddleClickInput;
            _inputActions.UI.ScrollWheel.performed -= OnScrollWheelInput;
            _inputActions.UI.TrackedDevicePosition.performed -= OnTrackedDevicePositionInput;
            _inputActions.UI.TrackedDeviceOrientation.performed -= OnTrackedDeviceOrientationInput;
            
            // Console Actions
            _inputActions.Console.ToggleConsole.performed -= OnConsoleToggled;
            _inputActions.Console.Submit.performed -= OnConsoleSubmit;
            _inputActions.Console.TabComplete.performed -= OnConsoleTabbed;
            _inputActions.Console.HistoryUp.performed -= OnConsoleHistoryUp;
            _inputActions.Console.HistoryDown.performed -= OnConsoleHistoryDown;
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
            // Debug.Log($"[InputService] UI Scroll Wheel: {scrollValue}");
            
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

        #region Console Input Event Handlers

        private void OnConsoleToggled(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] *** CONSOLE TOGGLE DETECTED *** - Phase: {context.phase} - Time: {Time.time}");
    
            _eventSystem.Publish(new ConsoleToggleInputEvent(context.phase));
        }

        private void OnConsoleSubmit(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Console Submit - Phase: {context.phase}");
            
            _eventSystem.Publish(new ConsoleSubmitInputEvent(context.phase));
        }

        private void OnConsoleTabbed(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Console Tab Complete - Phase: {context.phase}");
            
            _eventSystem.Publish(new ConsoleTabCompleteInputEvent(context.phase));
        }

        private void OnConsoleHistoryUp(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Console History Up - Phase: {context.phase}");
            
            _eventSystem.Publish(new ConsoleHistoryUpInputEvent(context.phase));
        }

        private void OnConsoleHistoryDown(InputAction.CallbackContext context)
        {
            Debug.Log($"[InputService] Console History Down - Phase: {context.phase}");
            
            _eventSystem.Publish(new ConsoleHistoryDownInputEvent(context.phase));
        }

        #endregion

        #region Input System Methods
        
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
        /// Get current mouse position
        /// </summary>
        public Vector2 GetMousePosition() => _lastMousePosition;
        
        /// <summary>
        /// Enable specific action map (legacy method - use SetInputContext instead)
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
                case "console":
                    _inputActions.Console.Enable();
                    Debug.Log("[InputService] Enabled Console action map");
                    break;
                default:
                    Debug.LogWarning($"[InputService] Unknown action map: {mapName}");
                    break;
            }
        }
        
        /// <summary>
        /// Disable specific action map (legacy method - use SetInputContext instead)
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
                case "console":
                    _inputActions.Console.Disable();
                    Debug.Log("[InputService] Disabled Console action map");
                    break;
                default:
                    Debug.LogWarning($"[InputService] Unknown action map: {mapName}");
                    break;
            }
        }

        /// <summary>
        /// Enable console input (only console actions, not toggle)
        /// Deprecated - use SetConsoleInputEnabled instead
        /// </summary>
        public void EnableConsoleInput(bool enable)
        {
            SetConsoleInputEnabled(enable);
        }
        
        #endregion
    }
}
