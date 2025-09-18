using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.Input;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.UI.Screens;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Loading state that manages loading UI and transitions
    /// Focuses solely on state management and UI - actual loading handled by LoadService
    /// Listens for loading completion events and transitions accordingly
    /// </summary>
    public class LoadingState : BaseGameState
    {
        #region Private Fields
        private const float MINIMUM_LOADING_TIME = 1.0f; // Minimum time to show loading screen
        private const float COMPLETION_BUFFER_TIME = 0.5f; // Extra time after completion
        private float _loadingStartTime;
        private bool _loadingCompleted = false;
        private bool _loadingFailed = false;
        private LoadingType _currentLoadingType = LoadingType.LoadSave; // Default to load save
        #endregion
        
        public LoadingState(
            GameContext context,
            IGameStateMachine stateMachine)
            : base(GameStateType.Loading, context, stateMachine)
        {
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            InputManager.SetInputContext(InputContext.UI);

            _loadingStartTime = Time.time;
            _loadingCompleted = false;
            _loadingFailed = false;
            
            // Subscribe to loading events
            EventSystem.Subscribe<LoadingCompletedEvent>(OnLoadingCompleted);
            EventSystem.Subscribe<LoadingFailedEvent>(OnLoadingFailed);
            EventSystem.Subscribe<BeginLoadGameEvent>(OnBeginLoadGame);
            EventSystem.Subscribe<BeginNewGameLoadEvent>(OnBeginNewGameLoad);
            
            // Show loading screen
            await UIService.ShowScreenAsync<LoadingScreen>();
            var loadingScreen = UIService.GetScreen<LoadingScreen>();
            loadingScreen?.SetLoadingType(_currentLoadingType);
            
            Debug.Log("[LoadingState] Loading state entered, waiting for loading completion...");
        }

        #region Event Handlers
        /// <summary>
        /// Handles loading completion - transitions to playing after buffer time
        /// </summary>
        private async void OnLoadingCompleted(LoadingCompletedEvent evt)
        {
            if (_loadingCompleted || _loadingFailed) return;
            
            _loadingCompleted = true;
            Debug.Log("[LoadingState] Loading completed, preparing to transition to playing...");
            
            // Ensure minimum loading time has passed
            await EnsureMinimumLoadingTime();
            
            // Add completion buffer for UX
            await Task.Delay((int)(COMPLETION_BUFFER_TIME * 1000));
            
            // Transition to playing state
            Debug.Log("[LoadingState] Transitioning to playing state...");
            await TransitionToStateAsync(GameStateType.Playing);
        }

        /// <summary>
        /// Handles when a load save game event begins - updates loading type
        /// </summary>
        private void OnBeginLoadGame(BeginLoadGameEvent evt)
        {
            _currentLoadingType = LoadingType.LoadSave;
            var loadingScreen = UIService.GetScreen<LoadingScreen>();
            loadingScreen?.SetLoadingType(_currentLoadingType);
            Debug.Log("[LoadingState] Load save game detected, setting loading type to LoadSave");
        }

        /// <summary>
        /// Handles when a new game load event begins - updates loading type
        /// </summary>
        private void OnBeginNewGameLoad(BeginNewGameLoadEvent evt)
        {
            _currentLoadingType = LoadingType.NewGame;
            var loadingScreen = UIService.GetScreen<LoadingScreen>();
            loadingScreen?.SetLoadingType(_currentLoadingType);
            Debug.Log("[LoadingState] New game load detected, setting loading type to NewGame");
        }

        /// <summary>
        /// Handles loading failure - shows error and returns to main menu
        /// </summary>
        private async void OnLoadingFailed(LoadingFailedEvent evt)
        {
            if (_loadingCompleted || _loadingFailed) return;
            
            _loadingFailed = true;
            Debug.LogError($"[LoadingState] Loading failed: {evt.Exception?.Message}");
            
            // Show error on loading screen
            var loadingScreen = UIService.GetScreen<LoadingScreen>();
            loadingScreen?.ShowError($"Loading failed: {evt.Exception?.Message ?? "Unknown error"}");
            
            // Wait a bit to show the error
            await Task.Delay(3000);
            
            // Return to main menu
            Debug.Log("[LoadingState] Returning to main menu due to loading failure...");
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Ensures minimum loading time has passed for good UX
        /// </summary>
        private async Task EnsureMinimumLoadingTime()
        {
            var elapsed = Time.time - _loadingStartTime;
            var remaining = MINIMUM_LOADING_TIME - elapsed;
            
            if (remaining > 0)
            {
                Debug.Log($"[LoadingState] Waiting additional {remaining:F1}s for minimum loading time...");
                await Task.Delay((int)(remaining * 1000));
            }
        }
        #endregion
        
        public override async Task ExitAsync()
        {
            // Unsubscribe from events
            EventSystem.Unsubscribe<LoadingCompletedEvent>(OnLoadingCompleted);
            EventSystem.Unsubscribe<LoadingFailedEvent>(OnLoadingFailed);
            EventSystem.Unsubscribe<BeginLoadGameEvent>(OnBeginLoadGame);
            EventSystem.Unsubscribe<BeginNewGameLoadEvent>(OnBeginNewGameLoad);
            
            // Hide loading screen
            await UIService.HideScreenAsync<LoadingScreen>();
            
            await base.ExitAsync();
        }
    }
}
