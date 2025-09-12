using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.UI.Screens;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Credits state - manages the credits screen and handles navigation
    /// Responsible for its UI lifecycle and event handling
    /// 
    /// Intent: Display game credits with appropriate audio and handle user navigation
    /// Design: Event-driven state management with clean separation of concerns
    /// Pros: Clear responsibility separation, maintainable, follows established patterns
    /// Cons: Requires careful event subscription management
    /// </summary>
    public class CreditsState : BaseGameState
    {
        private readonly IEventSystem _eventSystem;
        
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public CreditsState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputManager,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.Credits, stateMachine, eventSystem, audioService, uiService, inputManager, consoleService, gameDataService)
        {
            _eventSystem = eventSystem;
        }
        
        /// <summary>
        /// Enter credits state - setup UI, audio, input, and event subscriptions
        /// </summary>
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            InputManager.SetInputContext(InputContext.UI);
            
            // Subscribe to user interaction events from UI
            _eventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // Show the credits screen
            await UIService.ShowScreenAsync<CreditsScreen>();
            
            // Play appropriate music for credits
            //AudioService.PlayMusic("credits");
        }
        
        /// <summary>
        /// Exit credits state - cleanup UI, audio, and unsubscribe from events
        /// </summary>
        public override async Task ExitAsync()
        {
            // Unsubscribe from events to prevent memory leaks
            _eventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // Hide the credits screen
            await UIService.HideScreenAsync<CreditsScreen>();
            
            // Stop credits music
            //AudioService.StopMusic();
            
            await base.ExitAsync();
        }
        
        /// <summary>
        /// Handle main menu request from UI - transition back to main menu
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
    }
}
