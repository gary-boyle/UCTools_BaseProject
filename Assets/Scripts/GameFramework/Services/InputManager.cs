using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input.Handlers;
using GameFramework.Input.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.Input
{
    /// <summary>
    /// Unified Input Manager - handles both Unity Input System integration AND input handler management
    /// Now includes input settings integration for mouse sensitivity and Y-axis inversion
    /// Replaces both InputService and the old InputManager concept
    /// </summary>
    public class InputManager : IInputManager
    {
        // Store delegate references for proper unsubscription
        private System.Action<InputAction.CallbackContext> _onMoveInput;
        private System.Action<InputAction.CallbackContext> _onLookInput;
        private System.Action<InputAction.CallbackContext> _onAttackInput;
        private System.Action<InputAction.CallbackContext> _onJumpInput;
        private System.Action<InputAction.CallbackContext> _onPauseInput;
        private System.Action<InputAction.CallbackContext> _onInteractInput;
        private System.Action<InputAction.CallbackContext> _onCrouchInput;
        private System.Action<InputAction.CallbackContext> _onSprintInput;
        private System.Action<InputAction.CallbackContext> _onPreviousInput;
        private System.Action<InputAction.CallbackContext> _onNextInput;
        
        private System.Action<InputAction.CallbackContext> _onUINavigateInput;
        private System.Action<InputAction.CallbackContext> _onUISubmitInput;
        private System.Action<InputAction.CallbackContext> _onUICancelInput;
        private System.Action<InputAction.CallbackContext> _onUIClickInput;
        private System.Action<InputAction.CallbackContext> _onUIPointInput;
        private System.Action<InputAction.CallbackContext> _onUIRightClickInput;
        private System.Action<InputAction.CallbackContext> _onUIMiddleClickInput;
        private System.Action<InputAction.CallbackContext> _onUIScrollWheelInput;
        
        private System.Action<InputAction.CallbackContext> _onConsoleToggleInput;
        private System.Action<InputAction.CallbackContext> _onConsoleSubmitInput;
        private System.Action<InputAction.CallbackContext> _onConsoleTabCompleteInput;
        private System.Action<InputAction.CallbackContext> _onConsoleHistoryUpInput;
        private System.Action<InputAction.CallbackContext> _onConsoleHistoryDownInput;
        
        public bool IsInitialized { get; private set; }
        
        private readonly List<InputHandlerBase> _handlers = new();
        private readonly List<InputHandlerBase> _activeHandlers = new();
        private readonly IEventSystem _eventSystem;
        private readonly IConfigService _configService;
        private InputContext _currentContext = InputContext.None;
        
        // Unity Input System
        private InputSystem_Actions _inputActions;
        
        // Handler references for registration
        private readonly ConsoleInputHandler _consoleHandler;
        private readonly UIInputHandler _uiHandler;
        private readonly PlayerInputHandler _playerHandler;
        
        // Input settings state
        private float _mouseSensitivity = 1.0f;
        private bool _invertYAxis = false;
        
        public InputManager(
            IEventSystem eventSystem,
            IConfigService configService,
            ConsoleInputHandler consoleHandler,
            UIInputHandler uiHandler,
            PlayerInputHandler playerHandler)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _consoleHandler = consoleHandler ?? throw new ArgumentNullException(nameof(consoleHandler));
            _uiHandler = uiHandler ?? throw new ArgumentNullException(nameof(uiHandler));
            _playerHandler = playerHandler ?? throw new ArgumentNullException(nameof(playerHandler));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            try
            {
                // Initialize Unity Input System
                _inputActions = new InputSystem_Actions();
                SubscribeToUnityInputEvents();
                _inputActions.Enable(); // Enable all - handlers will decide what to process
                
                // Register input handlers
                RegisterHandler(_consoleHandler);
                RegisterHandler(_uiHandler);
                RegisterHandler(_playerHandler);
                
                // Subscribe to config changes for input settings
                _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);
                
                // Apply initial input settings
                ApplyInputSettings();
                
                // Set initial context (console always active)
                SetInputContext(InputContext.None);
                ActivateHandler<ConsoleInputHandler>();
                
                IsInitialized = true;
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputManager] Failed to initialize: {ex.Message}");
                throw;
            }
        }
        
        #region Input Settings Integration

        /// <summary>
        /// Handle options changed events for input settings
        /// </summary>
        private void OnOptionsChanged(OptionsChangedEvent evt)
        {
            ApplyInputSettings();
        }

        /// <summary>
        /// Apply current input settings from config
        /// </summary>
        private void ApplyInputSettings()
        {
            try
            {
                _mouseSensitivity = _configService.GetConfigValue<float>("input.mouse_sensitivity");
                _invertYAxis = _configService.GetConfigValue<bool>("input.invert_y_axis");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputManager] Error applying input settings: {ex.Message}");
                
                // Use default values on error
                _mouseSensitivity = 1.0f;
                _invertYAxis = false;
            }
        }

        /// <summary>
        /// Apply mouse sensitivity and Y-axis inversion to look input
        /// </summary>
        private Vector2 ProcessLookInput(Vector2 rawInput)
        {
            // Apply mouse sensitivity
            Vector2 processedInput = rawInput * _mouseSensitivity;
            
            // Apply Y-axis inversion
            if (_invertYAxis)
            {
                processedInput.y = -processedInput.y;
            }
            
            return processedInput;
        }

        /// <summary>
        /// Get current mouse sensitivity setting
        /// </summary>
        public float GetMouseSensitivity() => _mouseSensitivity;

        /// <summary>
        /// Get current Y-axis inversion setting
        /// </summary>
        public bool GetInvertYAxis() => _invertYAxis;

        #endregion
        
        #region Unity Input System Integration
        
        private void SubscribeToUnityInputEvents()
        {
            // Create delegate references
            _onMoveInput = ctx => _eventSystem.Publish(new PlayerMoveInputEvent(ctx.ReadValue<Vector2>(), ctx.phase));
            _onLookInput = ctx => {
                // Apply input settings to look input
                Vector2 rawInput = ctx.ReadValue<Vector2>();
                Vector2 processedInput = ProcessLookInput(rawInput);
                _eventSystem.Publish(new PlayerLookInputEvent(processedInput, ctx.phase));
            };
            _onAttackInput = ctx => _eventSystem.Publish(new PlayerAttackInputEvent(ctx.phase));
            _onJumpInput = ctx => _eventSystem.Publish(new PlayerJumpInputEvent());
            _onPauseInput = ctx => _eventSystem.Publish(new PlayerPauseInputEvent(ctx.phase));
            _onInteractInput = ctx => _eventSystem.Publish(new PlayerInteractInputEvent(ctx.phase));
            _onCrouchInput = ctx => _eventSystem.Publish(new PlayerCrouchInputEvent(ctx.phase));
            _onSprintInput = ctx => _eventSystem.Publish(new PlayerSprintInputEvent(ctx.phase));
            _onPreviousInput = ctx => _eventSystem.Publish(new PlayerPreviousInputEvent());
            _onNextInput = ctx => _eventSystem.Publish(new PlayerNextInputEvent());
            
            _onUINavigateInput = ctx => _eventSystem.Publish(new UINavigateInputEvent(ctx.ReadValue<Vector2>()));
            _onUISubmitInput = ctx => _eventSystem.Publish(new UISubmitInputEvent());
            _onUICancelInput = ctx => _eventSystem.Publish(new UICancelInputEvent());
            _onUIClickInput = ctx => _eventSystem.Publish(new UIClickInputEvent(ctx.phase));
            _onUIPointInput = ctx => _eventSystem.Publish(new UIPointInputEvent(ctx.ReadValue<Vector2>()));
            _onUIRightClickInput = ctx => _eventSystem.Publish(new UIRightClickInputEvent());
            _onUIMiddleClickInput = ctx => _eventSystem.Publish(new UIMiddleClickInputEvent());
            _onUIScrollWheelInput = ctx => _eventSystem.Publish(new UIScrollWheelInputEvent(ctx.ReadValue<Vector2>()));
            
            _onConsoleToggleInput = ctx => _eventSystem.Publish(new ConsoleToggleInputEvent(ctx.phase));
            _onConsoleSubmitInput = ctx => _eventSystem.Publish(new ConsoleSubmitInputEvent(ctx.phase));
            _onConsoleTabCompleteInput = ctx => _eventSystem.Publish(new ConsoleTabCompleteInputEvent(ctx.phase));
            _onConsoleHistoryUpInput = ctx => _eventSystem.Publish(new ConsoleHistoryUpInputEvent(ctx.phase));
            _onConsoleHistoryDownInput = ctx => _eventSystem.Publish(new ConsoleHistoryDownInputEvent(ctx.phase));
            
            // Subscribe using the delegate references
            // Player Actions
            _inputActions.Player.Move.performed += _onMoveInput;
            _inputActions.Player.Look.performed += _onLookInput;
            _inputActions.Player.Attack.performed += _onAttackInput;
            _inputActions.Player.Jump.performed += _onJumpInput;
            _inputActions.Player.Pause.performed += _onPauseInput;
            _inputActions.Player.Interact.performed += _onInteractInput;
            _inputActions.Player.Crouch.performed += _onCrouchInput;
            _inputActions.Player.Sprint.performed += _onSprintInput;
            _inputActions.Player.Previous.performed += _onPreviousInput;
            _inputActions.Player.Next.performed += _onNextInput;
            
            // UI Actions
            _inputActions.UI.Navigate.performed += _onUINavigateInput;
            _inputActions.UI.Submit.performed += _onUISubmitInput;
            _inputActions.UI.Cancel.performed += _onUICancelInput;
            _inputActions.UI.Click.performed += _onUIClickInput;
            _inputActions.UI.Point.performed += _onUIPointInput;
            _inputActions.UI.RightClick.performed += _onUIRightClickInput;
            _inputActions.UI.MiddleClick.performed += _onUIMiddleClickInput;
            _inputActions.UI.ScrollWheel.performed += _onUIScrollWheelInput;
            
            // Console Actions
            _inputActions.Console.ToggleConsole.performed += _onConsoleToggleInput;
            _inputActions.Console.Submit.performed += _onConsoleSubmitInput;
            _inputActions.Console.TabComplete.performed += _onConsoleTabCompleteInput;
            _inputActions.Console.HistoryUp.performed += _onConsoleHistoryUpInput;
            _inputActions.Console.HistoryDown.performed += _onConsoleHistoryDownInput;
        }
        
        private void UnsubscribeFromUnityInputEvents()
        {
            if (_inputActions == null) return;
            
            // Unsubscribe Player Actions
            if (_onMoveInput != null)
                _inputActions.Player.Move.performed -= _onMoveInput;
            if (_onLookInput != null)
                _inputActions.Player.Look.performed -= _onLookInput;
            if (_onAttackInput != null)
                _inputActions.Player.Attack.performed -= _onAttackInput;
            if (_onJumpInput != null)
                _inputActions.Player.Jump.performed -= _onJumpInput;
            if (_onPauseInput != null)
                _inputActions.Player.Pause.performed -= _onPauseInput;
            if (_onInteractInput != null)
                _inputActions.Player.Interact.performed -= _onInteractInput;
            if (_onCrouchInput != null)
                _inputActions.Player.Crouch.performed -= _onCrouchInput;
            if (_onSprintInput != null)
                _inputActions.Player.Sprint.performed -= _onSprintInput;
            if (_onPreviousInput != null)
                _inputActions.Player.Previous.performed -= _onPreviousInput;
            if (_onNextInput != null)
                _inputActions.Player.Next.performed -= _onNextInput;
            
            // Unsubscribe UI Actions
            if (_onUINavigateInput != null)
                _inputActions.UI.Navigate.performed -= _onUINavigateInput;
            if (_onUISubmitInput != null)
                _inputActions.UI.Submit.performed -= _onUISubmitInput;
            if (_onUICancelInput != null)
                _inputActions.UI.Cancel.performed -= _onUICancelInput;
            if (_onUIClickInput != null)
                _inputActions.UI.Click.performed -= _onUIClickInput;
            if (_onUIPointInput != null)
                _inputActions.UI.Point.performed -= _onUIPointInput;
            if (_onUIRightClickInput != null)
                _inputActions.UI.RightClick.performed -= _onUIRightClickInput;
            if (_onUIMiddleClickInput != null)
                _inputActions.UI.MiddleClick.performed -= _onUIMiddleClickInput;
            if (_onUIScrollWheelInput != null)
                _inputActions.UI.ScrollWheel.performed -= _onUIScrollWheelInput;
            
            // Unsubscribe Console Actions
            if (_onConsoleToggleInput != null)
                _inputActions.Console.ToggleConsole.performed -= _onConsoleToggleInput;
            if (_onConsoleSubmitInput != null)
                _inputActions.Console.Submit.performed -= _onConsoleSubmitInput;
            if (_onConsoleTabCompleteInput != null)
                _inputActions.Console.TabComplete.performed -= _onConsoleTabCompleteInput;
            if (_onConsoleHistoryUpInput != null)
                _inputActions.Console.HistoryUp.performed -= _onConsoleHistoryUpInput;
            if (_onConsoleHistoryDownInput != null)
                _inputActions.Console.HistoryDown.performed -= _onConsoleHistoryDownInput;
            
            // Clear delegate references
            _onMoveInput = null;
            _onLookInput = null;
            _onAttackInput = null;
            _onJumpInput = null;
            _onPauseInput = null;
            _onInteractInput = null;
            _onCrouchInput = null;
            _onSprintInput = null;
            _onPreviousInput = null;
            _onNextInput = null;
            
            _onUINavigateInput = null;
            _onUISubmitInput = null;
            _onUICancelInput = null;
            _onUIClickInput = null;
            _onUIPointInput = null;
            _onUIRightClickInput = null;
            _onUIMiddleClickInput = null;
            _onUIScrollWheelInput = null;
            
            _onConsoleToggleInput = null;
            _onConsoleSubmitInput = null;
            _onConsoleTabCompleteInput = null;
            _onConsoleHistoryUpInput = null;
            _onConsoleHistoryDownInput = null;
        }
        
        #endregion
        
        #region Handler Management (Core InputManager responsibility)
        
        public void RegisterHandler(InputHandlerBase handler)
        {
            if (handler == null) 
            {
                Debug.LogError("[InputManager] Cannot register null handler");
                return;
            }
            
            _handlers.Add(handler);
            _handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
        
        public void ActivateHandler<T>() where T : InputHandlerBase
        {
            if (!IsInitialized) return;
            
            var handler = _handlers.OfType<T>().FirstOrDefault();
            if (handler != null && !_activeHandlers.Contains(handler))
            {
                handler.Activate();
                _activeHandlers.Add(handler);
                _activeHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }
        
        public void DeactivateHandler<T>() where T : InputHandlerBase
        {
            if (!IsInitialized) return;
            
            var handler = _activeHandlers.OfType<T>().FirstOrDefault();
            if (handler != null)
            {
                handler.Deactivate();
                _activeHandlers.Remove(handler);
            }
        }
        
        public void SetInputContext(InputContext context)
        {
            if (!IsInitialized || _currentContext == context) return;
            
            _currentContext = context;
            
            // Deactivate non-console handlers
            DeactivateHandler<UIInputHandler>();
            DeactivateHandler<PlayerInputHandler>();
            
            // Activate based on context
            switch (context)
            {
                case InputContext.UI:
                    ActivateHandler<UIInputHandler>();
                    break;
                case InputContext.Player:
                    ActivateHandler<PlayerInputHandler>();
                    break;
                case InputContext.Mixed:
                    ActivateHandler<UIInputHandler>();
                    ActivateHandler<PlayerInputHandler>();
                    break;
            }
        }

        public InputContext GetCurrentContext() => _currentContext;
        
        #endregion
        
        public void Update()
        {
            // Input handlers manage themselves via events
            // Could add input buffering, conflict detection, etc. here if needed
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            // Unsubscribe from config changes
            _eventSystem.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
            
            // Shutdown handlers
            foreach (var handler in _activeHandlers.ToList())
            {
                handler.Deactivate();
            }
            
            // Shutdown Unity Input System
            UnsubscribeFromUnityInputEvents();
            _inputActions?.Disable();
            _inputActions?.Dispose();
            
            // Clear state
            _activeHandlers.Clear();
            _handlers.Clear();
            _currentContext = InputContext.None;
            
            // Reset input settings
            _mouseSensitivity = 1.0f;
            _invertYAxis = false;
            
            IsInitialized = false;
        }
    }
}
