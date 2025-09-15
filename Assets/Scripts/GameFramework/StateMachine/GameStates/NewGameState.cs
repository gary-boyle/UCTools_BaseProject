using System.Threading.Tasks;
using UnityEngine;
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
    /// New Game state - fully responsible for its UI lifecycle
    /// Handles all UI transitions based on user interactions reported by the screen
    /// Initializes new game data through GameDataService
    /// </summary>
    public class NewGameState : BaseGameState
    {
        #region Private Fields
        private IGameDataService _gameDataService;
        #endregion

        public NewGameState(
            GameContext context,
            IGameStateMachine stateMachine)
            : base(GameStateType.NewGame, context, stateMachine)
        {
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Get GameDataService dependency from context
            _gameDataService = context.GameDataService;

            InputManager.SetInputContext(InputContext.UI);

            // Subscribe to user interaction events from UI
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // State is responsible for showing its UI
            await UIService.ShowScreenAsync<NewGameScreen>();
        }
        
        /// <summary>
        /// Handle new game creation - initializes game data and transitions to loading
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            try
            {
                Debug.Log($"[NewGameState] Creating new game - Player: {evt.PlayerName}, Difficulty: {evt.Difficulty}");

                // Initialize new game data through GameDataService
                _gameDataService.StartNewGame(
                    playerName: evt.PlayerName ?? "Player",
                    difficulty: evt.Difficulty ?? "Normal",
                    startingScene: evt.StartingScene ?? "GameLevel1"
                );

                Debug.Log("[NewGameState] New game data initialized successfully");

                
                // Transition to loading state to load the game scene
                //await TransitionToStateAsync(GameStateType.Loading);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NewGameState] Failed to create new game: {ex.Message}");
                // Could show error dialog here or transition back to main menu
                await TransitionToStateAsync(GameStateType.MainMenu);
            }
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
            
            // Clear service reference
            _gameDataService = null;
            
            // State is responsible for cleaning up its UI
            await UIService.HideScreenAsync<NewGameScreen>();
            
            await base.ExitAsync();
        }
    }
}
