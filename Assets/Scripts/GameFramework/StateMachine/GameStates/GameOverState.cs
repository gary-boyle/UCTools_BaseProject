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
    /// Game Over state - manages the game over screen and handles player choices
    /// Responsible for its UI lifecycle and event handling
    /// </summary>
    public class GameOverState : BaseGameState
    {
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public GameOverState(
            GameContext context,
            IGameStateMachine stateMachine)
            : base(GameStateType.GameOver, context, stateMachine)
        {
        }
        
        /// <summary>
        /// Enter game over state - setup UI, audio, input, and event subscriptions
        /// </summary>
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Set input context for UI navigation
            InputManager.SetInputContext(InputContext.UI);
            
            // Subscribe to user interaction events from UI
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Subscribe<LoadWindowRequestedEvent>(OnLoadWindowRequested);
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // Show the game over screen
            await UIService.ShowScreenAsync<GameOverScreen>();
            
            // Play appropriate music/sound for game over
            //AudioService.PlayMusic("gameover");
        }
        
        /// <summary>
        /// Exit game over state - cleanup UI, audio, and unsubscribe from events
        /// </summary>
        public override async Task ExitAsync()
        {
            // Unsubscribe from events to prevent memory leaks
            EventSystem.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Unsubscribe<LoadWindowRequestedEvent>(OnLoadWindowRequested);
            EventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // Hide the game over screen
            await UIService.HideScreenAsync<GameOverScreen>();
            
            // Stop game over music
            //AudioService.StopMusic();
            
            await base.ExitAsync();
        }
        
        #region Event Handlers
        
        /// <summary>
        /// Handle new game request - restart the game
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.NewGame);
        }
        
        /// <summary>
        /// Handle load window request - show load game interface
        /// </summary>
        private async void OnLoadWindowRequested(LoadWindowRequestedEvent evt)
        {
            await UIService.ShowPopupAsync<LoadGamePopup>();
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
