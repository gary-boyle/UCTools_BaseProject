using GameFramework.Config.ScriptableObjects;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;

namespace GameFramework.Input.Handlers
{
    /// <summary>
    /// Handles console input - always active, highest priority
    /// When console is open, consumes most input to prevent conflicts
    /// </summary>
    public class ConsoleInputHandler : InputHandlerBase
    {
        private readonly IConsoleService _consoleService;
        
        public ConsoleInputHandler(IEventSystem eventSystem, IConsoleService consoleService)
            : base("Console", 1000, eventSystem) // Highest priority
        {
            _consoleService = consoleService;
        }
        
        protected override void SubscribeToEvents()
        {
            _eventSystem.Subscribe<ConsoleToggleInputEvent>(OnConsoleToggle);
            _eventSystem.Subscribe<ConsoleSubmitInputEvent>(OnConsoleSubmit);
            _eventSystem.Subscribe<ConsoleTabCompleteInputEvent>(OnConsoleTab);
        }
        
        protected override void UnsubscribeFromEvents()
        {
            _eventSystem.Unsubscribe<ConsoleToggleInputEvent>(OnConsoleToggle);
            _eventSystem.Unsubscribe<ConsoleSubmitInputEvent>(OnConsoleSubmit);
            _eventSystem.Unsubscribe<ConsoleTabCompleteInputEvent>(OnConsoleTab);
        }
        
        public override bool HandleInput<T>(T inputEvent)
        {
            // Console toggle always works
            if (inputEvent is ConsoleToggleInputEvent)
                return false; // Let it be handled by our event handler, don't consume
            
            // If console is open, consume most other input to prevent conflicts
            if (!_consoleService.IsConsoleOpen()) return false; // Console closed, don't consume input

            // Allow console-specific input through
            return inputEvent is not ConsoleSubmitInputEvent && 
                   inputEvent is not ConsoleTabCompleteInputEvent;
        }
        
        private void OnConsoleToggle(ConsoleToggleInputEvent evt)
        {
            if(SettingsRegistry.Get<DebugSettings_SO>().ConsoleEnabled.Value)
            {
                _consoleService.SetConsoleOpen(!_consoleService.IsConsoleOpen());
            }
        }
        
        private static void OnConsoleSubmit(ConsoleSubmitInputEvent evt)
        {
        }
        
        private static void OnConsoleTab(ConsoleTabCompleteInputEvent evt)
        {
        }
    }
}
