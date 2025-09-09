using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Interfaces;
using UCTools_CommandConsole;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Service that orchestrates the debug console system using dependency injection.
    /// 
    /// Architecture:
    /// - Manages console state (open/closed)
    /// - Coordinates between input system and console GUI
    /// - Handles console toggle events
    /// - Provides public API for other systems to interact with console
    /// - Respects debug settings from configuration
    /// 
    /// Flow:
    /// 1. Receives toggle input events from InputService
    /// 2. Checks if console is enabled in debug settings
    /// 3. Updates console open/closed state only if enabled
    /// 4. Notifies InputService to enable/disable console input actions
    /// 5. Delegates UI updates to ConsoleGUI via Console static class
    /// </summary>
    public class ConsoleService : IConsoleService, IUpdatable, ILateUpdatable
    {
        #region Dependencies
        private readonly ConsoleGUI _consoleGUI;
        private readonly IEventSystem _eventSystem;
        private readonly IInputService _inputService;
        private readonly IConfigService _configService; // Added config service dependency
        #endregion

        #region State
        private bool _isInitialized = false;
        private bool _isConsoleOpen = false;
        #endregion

        #region Constants
        private const string LOG_PREFIX = "[ConsoleService]";
        private const string CONSOLE_ENABLED_CONFIG_KEY = "debug.console_enabled";
        private const string SHOW_DEBUG_INFO_CONFIG_KEY = "debug.show_debug_info";
        #endregion

        /// <summary>
        /// Constructor injection - DI container provides all dependencies
        /// </summary>
        public ConsoleService(ConsoleGUI consoleGUI, IEventSystem eventSystem, IInputService inputService, IConfigService configService)
        {
            _consoleGUI = consoleGUI ?? throw new System.ArgumentNullException(nameof(consoleGUI));
            _eventSystem = eventSystem ?? throw new System.ArgumentNullException(nameof(eventSystem));
            _inputService = inputService ?? throw new System.ArgumentNullException(nameof(inputService));
            _configService = configService ?? throw new System.ArgumentNullException(nameof(configService));
            
            Debug.Log($"{LOG_PREFIX} Created with injected dependencies");
        }

        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initialize the console service and set up event subscriptions
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"{LOG_PREFIX} Already initialized");
                return;
            }

            Debug.Log($"{LOG_PREFIX} Initializing console service...");

            try
            {
                // Initialize the console system with our GUI
                Console.Init(_consoleGUI);

                // Register built-in commands
                RegisterBuiltInCommands();

                // Subscribe to console toggle events from input system
                _eventSystem.Subscribe<ConsoleToggleInputEvent>(OnConsoleToggleEvent);

                // Subscribe to configuration changes to react to debug setting changes
                _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);

                _isInitialized = true;
                Debug.Log($"{LOG_PREFIX} Console service initialized successfully");

                await Task.Yield(); // Ensure initialization completes before next frame
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{LOG_PREFIX} Failed to initialize: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clean shutdown of console service
        /// </summary>
        public void Shutdown()
        {
            if (!_isInitialized) return;

            Debug.Log($"{LOG_PREFIX} Shutting down console service...");
            
            // Unsubscribe from events
            _eventSystem?.Unsubscribe<ConsoleToggleInputEvent>(OnConsoleToggleEvent);
            _eventSystem?.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
            
            // Shutdown console system
            Console.Shutdown();
            
            // Reset state
            _isInitialized = false;
            _isConsoleOpen = false;
        }

        /// <summary>
        /// Update console system - processes pending commands
        /// </summary>
        public void Update()
        {
            if (!_isInitialized) return;
            
            // Update the console system (processes command queue)
            Console.ConsoleUpdate();
        }

        /// <summary>
        /// Late update for console UI (handles caret positioning after UI events)
        /// </summary>
        public void LateUpdate()
        {
            if (!_isInitialized) return;
            
            // Late update for console UI
            Console.ConsoleLateUpdate();
        }

        #region Public API

        /// <summary>
        /// Check if console is currently open
        /// </summary>
        public bool IsConsoleOpen() => _isInitialized && _isConsoleOpen;

        /// <summary>
        /// Check if console is enabled in configuration
        /// </summary>
        public bool IsConsoleEnabled()
        {
            if (!_isInitialized) return false;
            
            // Check both console_enabled and show_debug_info settings
            var consoleEnabled = _configService.GetConfigValue<bool>(CONSOLE_ENABLED_CONFIG_KEY);
            var debugInfoEnabled = _configService.GetConfigValue<bool>(SHOW_DEBUG_INFO_CONFIG_KEY);
            
            return consoleEnabled && debugInfoEnabled;
        }

        /// <summary>
        /// Programmatically open or close the console
        /// </summary>
        /// <param name="open">True to open, false to close</param>
        public void SetConsoleOpen(bool open)
        {
            if (!_isInitialized) 
            {
                Debug.LogWarning($"{LOG_PREFIX} Cannot set console open state - not initialized");
                return;
            }
            
            // Check if console is enabled before opening
            if (open && !IsConsoleEnabled())
            {
                Debug.Log($"{LOG_PREFIX} Console toggle ignored - console disabled in settings");
                return;
            }
            
            if (_isConsoleOpen == open) return; // No change needed
            
            Debug.Log($"{LOG_PREFIX} Setting console {(open ? "open" : "closed")}");
            
            _isConsoleOpen = open;
            
            // Update the console UI
            Console.SetOpen(open);
            
            // Enable/disable console input actions
            _inputService.SetConsoleInputEnabled(open);
        }

        /// <summary>
        /// Execute a command programmatically (bypasses input field)
        /// </summary>
        /// <param name="command">Command string to execute</param>
        public void ExecuteCommand(string command)
        {
            if (!_isInitialized) 
            {
                Debug.LogWarning($"{LOG_PREFIX} Cannot execute command - not initialized");
                return;
            }
            
            if (string.IsNullOrEmpty(command)) return;
            
            Console.EnqueueCommand(command);
        }

        /// <summary>
        /// Write a message to the console output
        /// </summary>
        /// <param name="message">Message to write</param>
        public void WriteLine(string message)
        {
            if (!_isInitialized) 
            {
                Debug.LogWarning($"{LOG_PREFIX} Cannot write to console - not initialized");
                return;
            }
            
            Console.Write(message ?? string.Empty);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle console toggle input events
        /// Only responds to 'Performed' phase and only if console is enabled in settings
        /// </summary>
        private void OnConsoleToggleEvent(ConsoleToggleInputEvent evt)
        {
            if (evt.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                Debug.Log($"{LOG_PREFIX} Console toggle event received - checking if console is enabled...");
                
                if (!IsConsoleEnabled())
                {
                    Debug.Log($"{LOG_PREFIX} Console toggle ignored - console disabled in debug settings");
                    return;
                }
                
                SetConsoleOpen(!_isConsoleOpen);
            }
        }

        /// <summary>
        /// Handle configuration changes - close console if it gets disabled
        /// </summary>
        private void OnOptionsChanged(OptionsChangedEvent evt)
        {
            // If console is currently open but gets disabled, close it
            if (_isConsoleOpen && !IsConsoleEnabled())
            {
                Debug.Log($"{LOG_PREFIX} Console disabled in settings - closing console");
                SetConsoleOpen(false);
            }
        }

        #endregion

        #region Command Registration

        /// <summary>
        /// Register default console commands
        /// These are basic utility commands available in all builds
        /// </summary>
        private void RegisterBuiltInCommands()
        {
            // TODO: Add built-in commands like:
            // - help: List all available commands
            // - clear: Clear console output
            // - quit: Quit application
            // - version: Show application version
            
            Debug.Log($"{LOG_PREFIX} Built-in commands registered");
        }

        #endregion
    }
}
