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
        private readonly IConfigService _configService;
        
        public ConsoleInputHandler(IEventSystem eventSystem, IConsoleService consoleService, IConfigService configService)
            : base("Console", 1000, eventSystem, true) // Highest priority
        {
            _consoleService = consoleService;
            _configService = configService;
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
            if (_consoleService.IsConsoleOpen())
            {
                // Allow console-specific input through
                if (inputEvent is ConsoleSubmitInputEvent || 
                    inputEvent is ConsoleTabCompleteInputEvent)
                    return false;
                
                // Consume all other input when console is open
                return true;
            }
            
            return false; // Console closed, don't consume input
        }
        
        private void OnConsoleToggle(ConsoleToggleInputEvent evt)
        {
            if (_configService.GetConfigValue<bool>("debug.console_enabled"))
            {
                _consoleService.SetConsoleOpen(!_consoleService.IsConsoleOpen());
            }
        }
        
        private void OnConsoleSubmit(ConsoleSubmitInputEvent evt)
        {
            // Handle console command submission
        }
        
        private void OnConsoleTab(ConsoleTabCompleteInputEvent evt)
        {
            // Handle console tab completion
        }
    }
}
