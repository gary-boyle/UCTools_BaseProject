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
    /// Service that manages the debug console system using dependency injection
    /// </summary>
    public class ConsoleService : IConsoleService, IUpdatable, ILateUpdatable
    {
        private readonly ConsoleGUI _consoleGUI;
        private readonly IEventSystem _eventSystem;
        private readonly IInputService _inputService;
        private bool _isInitialized = false;

        // Constructor injection - DI will provide the dependencies
        public ConsoleService(ConsoleGUI consoleGUI, IEventSystem eventSystem, IInputService inputService)
        {
            _consoleGUI = consoleGUI ?? throw new System.ArgumentNullException(nameof(consoleGUI));
            _eventSystem = eventSystem ?? throw new System.ArgumentNullException(nameof(eventSystem));
            _inputService = inputService ?? throw new System.ArgumentNullException(nameof(inputService));
            Debug.Log("[ConsoleService] ConsoleService created with injected dependencies");
        }

        public bool IsInitialized => _isInitialized;

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ConsoleService] Already initialized");
                return;
            }

            Debug.Log("[ConsoleService] Initializing console service...");

            try
            {
                // Initialize the static Console class with our injected GUI
                Console.Init(_consoleGUI);

                // Register default commands
                RegisterDefaultCommands();

                // Subscribe to console toggle events
                _eventSystem.Subscribe<ConsoleToggleInputEvent>(OnConsoleToggle);

                _isInitialized = true;
                Debug.Log("[ConsoleService] Console service initialized successfully");

                // Wait a frame to ensure everything is set up
                await Task.Yield();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ConsoleService] Failed to initialize: {e.Message}");
                throw;
            }
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;

            Debug.Log("[ConsoleService] Shutting down console service...");
            
            _eventSystem.Unsubscribe<ConsoleToggleInputEvent>(OnConsoleToggle);
            
            Console.Shutdown();
            _isInitialized = false;
        }

        public void Update()
        {
            if (!_isInitialized) return;
            
            // Update the console system
            Console.ConsoleUpdate();
        }

        public void LateUpdate()
        {
            if (!_isInitialized) return;
            
            // Late update for console
            Console.ConsoleLateUpdate();
        }

        public bool IsConsoleOpen()
        {
            return _isInitialized && Console.IsOpen();
        }

        public void SetConsoleOpen(bool open)
        {
            if (!_isInitialized) return;
            
            Console.SetOpen(open);
            
            // Enable/disable console input based on console state
            _inputService.SetConsoleInputEnabled(open);
        }

        public void ExecuteCommand(string command)
        {
            if (!_isInitialized) return;
            
            Console.EnqueueCommand(command);
        }

        public void WriteLine(string message)
        {
            if (!_isInitialized) return;
            
            Console.Write(message);
        }

        private void OnConsoleToggle(ConsoleToggleInputEvent evt)
        {
            Debug.Log($"[ConsoleService] Console toggle event received - Phase: {evt.Phase}");
    
            if (evt.Phase == UnityEngine.InputSystem.InputActionPhase.Performed)
            {
                var isOpen = IsConsoleOpen();
                Debug.Log($"[ConsoleService] Console is currently {(isOpen ? "open" : "closed")}, toggling to {(!isOpen ? "open" : "closed")}");
                SetConsoleOpen(!isOpen);
            }
        }

        /// <summary>
        /// Register default console commands
        /// </summary>
        private void RegisterDefaultCommands()
        {
            // Add some basic commands here
            // You can expand this based on your needs
            
            Debug.Log("[ConsoleService] Default commands registered");
        }
    }
}
