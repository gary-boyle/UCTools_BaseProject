using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.Input;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.UI.Screens;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Credits state - manages the credits screen and handles navigation
    /// Responsible for its UI lifecycle and event handling
    /// </summary>
    public class CreditsState : BaseGameState
    {
        
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public CreditsState(
            GameContext context,
            IGameStateMachine stateMachine)
            : base(GameStateType.Credits, context, stateMachine)
        {
        }
        
        /// <summary>
        /// Enter credits state - setup UI, audio, input, and event subscriptions
        /// </summary>
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            InputManager.SetInputContext(InputContext.UI);
            
            // Subscribe to user interaction events from UI
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
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
            EventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
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
