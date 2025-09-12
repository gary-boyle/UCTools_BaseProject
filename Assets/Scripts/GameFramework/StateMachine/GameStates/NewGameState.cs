using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.Input;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.UI.Screens;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// New Game state - fully responsible for its UI lifecycle
    /// Handles all UI transitions based on user interactions reported by the screen
    /// </summary>
    public class NewGameState : BaseGameState
    {
        public NewGameState(
            GameContext context,
            IGameStateMachine stateMachine)
            : base(GameStateType.NewGame, context, stateMachine)
        {
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            InputManager.SetInputContext(InputContext.UI);

            // Subscribe to user interaction events from UI
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // State is responsible for showing its UI
            await UIService.ShowScreenAsync<NewGameScreen>();
        }
        
        /// <summary>
        /// Handle new game creation - state manages UI transition to loading
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            // Create loading configuration
            var loadingConfig = LoadingConfiguration.NewGame(evt.StartingScene, evt.PlayerName);
            loadingConfig.GameData["difficulty"] = evt.Difficulty;
            
            foreach (var kvp in evt.CustomData)
            {
                loadingConfig.GameData[kvp.Key] = kvp.Value;
            }
            
            GameDataService.CurrentLoadingConfig = loadingConfig;
            await TransitionToStateAsync(GameStateType.Loading);
        }
        
        /// <summary>
        /// Handle back to main menu - state manages UI transition
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        public override async Task ExitAsync()
        {
            // Unsubscribe from events
            EventSystem.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // State is responsible for cleaning up its UI
            await UIService.HideScreenAsync<NewGameScreen>();
            
            await base.ExitAsync();
        }
    }
}
