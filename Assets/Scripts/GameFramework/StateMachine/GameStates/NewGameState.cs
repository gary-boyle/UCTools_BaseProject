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
    /// Initializes new game data through GameDataService and transitions to PlayingState
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
        /// Handle new game creation - initializes game data and transitions to PlayingState
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            if (_gameDataService == null)
            {
                Debug.LogError("[NewGameState] Cannot create new game - GameDataService not available");
                await TransitionToStateAsync(GameStateType.MainMenu);
                return;
            }

            try
            {
                Debug.Log($"[NewGameState] Creating new game - Player: {evt.PlayerName}, Difficulty: {evt.Difficulty}, Scene: {evt.StartingScene}");

                // Validate input parameters
                string playerName = string.IsNullOrWhiteSpace(evt.PlayerName) ? "Player" : evt.PlayerName.Trim();
                string difficulty = string.IsNullOrWhiteSpace(evt.Difficulty) ? "Normal" : evt.Difficulty;
                string startingScene = string.IsNullOrWhiteSpace(evt.StartingScene) ? "GameLevel1" : evt.StartingScene;

                // Initialize new game data through GameDataService
                _gameDataService.StartNewGame(
                    playerName: playerName,
                    difficulty: difficulty,
                    startingScene: startingScene
                );

                Debug.Log("[NewGameState] New game data initialized successfully");

                // Give a brief moment for UI feedback before transitioning
                await Task.Delay(100);

                // Transition directly to PlayingState to start the game
                Debug.Log("[NewGameState] Transitioning to PlayingState to start new game");
                await TransitionToStateAsync(GameStateType.Playing);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NewGameState] Failed to create new game: {ex.Message}");
                
                // Show error feedback to user (could add error UI here)
                Debug.LogError("[NewGameState] Returning to main menu due to new game creation failure");
                await TransitionToStateAsync(GameStateType.MainMenu);
            }
        }
        
        /// <summary>
        /// Handle back to main menu - state manages UI transition
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            Debug.Log("[NewGameState] Main menu requested, transitioning back");
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        public override async Task ExitAsync()
        {
            Debug.Log("[NewGameState] Exiting NewGameState");
            
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
