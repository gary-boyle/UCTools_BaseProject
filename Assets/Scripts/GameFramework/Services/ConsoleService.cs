using System.Threading.Tasks;
using GameFramework.Config.ScriptableObjects;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.ConsoleTool;
using GameFramework.Core;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Service that orchestrates the debug console system using dependency injection.
    /// Now integrates with debug settings for console and logging control.
    /// 
    /// Architecture:
    /// - Manages console state (open/closed) based on debug.console_enabled setting
    /// - Manages verbose logging based on debug.verbose_logging setting
    /// - Provides public API for other systems to interact with console
    /// - Input conflict management is handled by ConsoleInputHandler
    /// 
    /// Flow:
    /// 1. Receives toggle input events from InputManager
    /// 2. Checks if console is enabled in debug settings
    /// 3. Updates console open/closed state only if enabled
    /// 4. Applies logging settings when configuration changes
    /// 5. ConsoleInputHandler automatically manages input conflicts by checking IsConsoleOpen()
    /// </summary>
    public class ConsoleService : IConsoleService, IUpdatable, ILateUpdatable
    {
        #region Dependencies
        private readonly ConsoleGUI _consoleGUI;
        private readonly IEventSystem _eventSystem;
        #endregion

        #region State
        private bool _isInitialized = false;
        private bool _isConsoleOpen = false;
        #endregion

        #region Constants
        private const string LOG_PREFIX = "[ConsoleService]";
        private const string CONSOLE_ENABLED_CONFIG_KEY = "debug.console_enabled";
        private const string VERBOSE_LOGGING_CONFIG_KEY = "debug.verbose_logging";
        #endregion

        /// <summary>
        /// Constructor injection - DI container provides all dependencies
        /// </summary>
        public ConsoleService(ConsoleGUI consoleGUI, IEventSystem eventSystem)
        {
            _consoleGUI = consoleGUI ?? throw new System.ArgumentNullException(nameof(consoleGUI));
            _eventSystem = eventSystem ?? throw new System.ArgumentNullException(nameof(eventSystem));
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
    
            try
            {
                // Initialize the console system with our GUI
                Console.Init(_consoleGUI);
        
                // Register built-in commands
                RegisterBuiltInCommands();

                // NOTE: Removed ConsoleToggleInputEvent subscription - now handled by ConsoleInputHandler
                // This prevents duplicate event handling conflicts

                // Subscribe to configuration changes to react to debug setting changes
                _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);

                // Apply initial debug settings (console enabled/disabled, verbose logging)
                ApplyDebugSettings();

                _isInitialized = true;
        
                Debug.Log($"{LOG_PREFIX} Successfully initialized");

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
            
            // Check console enabled setting
            var consoleEnabled = SettingsRegistry.Get<DebugSettings_SO>().ConsoleEnabled.Value;
            
            return consoleEnabled;
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
                return;
            }
            
            if (_isConsoleOpen == open) return; // No change needed
            
            _isConsoleOpen = open;
            
            // Update the console UI
            Console.SetOpen(open);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle console toggle input events
        /// Only responds to 'Performed' phase and only if console is enabled in settings
        /// </summary>
        private void OnConsoleToggleEvent(ConsoleToggleInputEvent evt)
        {
            Debug.Log("!!!!");
            if (evt.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                if (!IsConsoleEnabled())
                {
                    return;
                }
                
                SetConsoleOpen(!_isConsoleOpen);
            }
        }

        /// <summary>
        /// Handle configuration changes - apply new debug settings
        /// </summary>
        private void OnOptionsChanged(OptionsChangedEvent evt)
        {
            ApplyDebugSettings();
        }

        #endregion

        #region Debug Settings Integration

        /// <summary>
        /// Apply current debug settings from config
        /// </summary>
        private void ApplyDebugSettings()
        {
            try
            {
                var consoleEnabled = SettingsRegistry.Get<DebugSettings_SO>().ConsoleEnabled.Value;
                var verboseLogging = SettingsRegistry.Get<DebugSettings_SO>().VerboseLogging.Value;
                
                SetConsoleEnabled(consoleEnabled);
                SetVerboseLogging(verboseLogging);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Error applying debug settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable or disable the console system
        /// </summary>
        private void SetConsoleEnabled(bool enabled)
        {
            // If console is currently open but gets disabled, close it
            if (_isConsoleOpen && !enabled)
            {
                SetConsoleOpen(false);
            }
            
            // Note: We don't need to store console enabled state separately
            // since we always check the config service in IsConsoleEnabled()
        }

        /// <summary>
        /// Set verbose logging level
        /// </summary>
        private void SetVerboseLogging(bool verbose)
        {
            try
            {
                // Apply logging level changes
                Debug.unityLogger.logEnabled = verbose;
                
                // Set filter levels for different log types
                if (verbose)
                {
                    // Enable all log types in verbose mode
                    Debug.unityLogger.filterLogType = LogType.Log;
                }
                else
                {
                    // Only show warnings and errors in non-verbose mode
                    Debug.unityLogger.filterLogType = LogType.Warning;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Error setting verbose logging: {ex.Message}");
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
            // - config: Show/set configuration values
            
            // Example implementation:
            // Console.RegisterCommand("help", "Show available commands", ShowHelp);
            // Console.RegisterCommand("clear", "Clear console output", ClearConsole);
            // Console.RegisterCommand("quit", "Quit application", QuitApplication);
        }
        
        #endregion
    }
}
