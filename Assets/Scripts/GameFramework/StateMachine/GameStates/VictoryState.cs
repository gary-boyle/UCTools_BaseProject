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
    /// Victory state - manages the victory screen and celebrates player success
    /// Responsible for its UI lifecycle and event handling
    /// </summary>
    public class VictoryState : BaseGameState
    {
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public VictoryState(
            GameContext context,
            IGameStateMachine stateMachine)
            : base(GameStateType.Victory, context, stateMachine)
        {
        }
        
        /// <summary>
        /// Enter victory state - setup UI, audio, input, and event subscriptions
        /// </summary>
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Set input context for UI navigation
            InputManager.SetInputContext(InputContext.UI);
            
            // Subscribe to user interaction events from UI
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // Show the victory screen
            await UIService.ShowScreenAsync<VictoryScreen>();
            
            // Handle victory completion tasks
            //await HandleVictoryCompletion(context);
        }
        
        /// <summary>
        /// Exit victory state - cleanup UI, audio, and unsubscribe from events
        /// </summary>
        public override async Task ExitAsync()
        {
            // Unsubscribe from events to prevent memory leaks
            EventSystem.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // Hide the victory screen
            await UIService.HideScreenAsync<VictoryScreen>();
            
            // Fade out victory music
            //AudioService.StopMusic();
            
            await base.ExitAsync();
        }
        
        #region Event Handlers
        
        /// <summary>
        /// Handle new game request - start a new game (potentially with progression)
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.NewGame);
        }
        
        /// <summary>
        /// Handle main menu request - return to main menu
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        #endregion
    }
}
