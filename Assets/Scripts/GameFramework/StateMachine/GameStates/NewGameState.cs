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
    /// Publishes new game load events and transitions to LoadingState for consistent loading experience
    /// </summary>
    public class NewGameState : BaseGameState
    {
        #region Private Fields
        // No longer need GameDataService dependency - LoadService handles game data setup
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

            InputManager.SetInputContext(InputContext.UI);

            // Subscribe to user interaction events from UI
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // State is responsible for showing its UI
            await UIService.ShowScreenAsync<NewGameScreen>();
        }
        
        /// <summary>
        /// Handle new game creation - publishes new game load event and transitions to LoadingState
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            try
            {
                Debug.Log($"[NewGameState] Starting new game - Player: {evt.PlayerName}, Difficulty: {evt.Difficulty}, Scene: {evt.StartingScene}");

                // Validate input parameters
                string playerName = string.IsNullOrWhiteSpace(evt.PlayerName) ? "Player" : evt.PlayerName.Trim();
                string difficulty = string.IsNullOrWhiteSpace(evt.Difficulty) ? "Normal" : evt.Difficulty;
                string startingScene = string.IsNullOrWhiteSpace(evt.StartingScene) ? "GameLevel1" : evt.StartingScene;

                // Give a brief moment for UI feedback before transitioning
                await Task.Delay(100);

                // Publish new game load event - LoadService will handle the actual loading
                var beginNewGameLoadEvent = new BeginNewGameLoadEvent(playerName, difficulty, startingScene);
                EventSystem.Publish(beginNewGameLoadEvent);

                Debug.Log("[NewGameState] New game load event published, transitioning to LoadingState");

                // Transition to LoadingState - this will now handle the loading process
                await TransitionToStateAsync(GameStateType.Loading);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NewGameState] Failed to start new game: {ex.Message}");
                
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
            
            // State is responsible for cleaning up its UI
            await UIService.HideScreenAsync<NewGameScreen>();
            
            await base.ExitAsync();
        }
    }
}
