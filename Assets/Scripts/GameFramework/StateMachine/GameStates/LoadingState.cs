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
    /// Loading state that handles different loading scenarios using unified GameSession system
    /// Creates or loads GameSession based on loading configuration
    /// Integrates with TimeService for proper playtime handling
    /// </summary>
    public class LoadingState : BaseGameState
    {
        private LoadingConfiguration _currentConfig;
        private float _loadingStartTime;
        
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
            
            // Show loading screen if requested
            if (_currentConfig.ShowLoadingScreen)
            {
                await UIService.ShowScreenAsync<LoadingScreen>();
            }
            
            // Start the appropriate loading process
            await ProcessLoadingConfiguration();
        }
        
        private async Task ProcessLoadingConfiguration()
        {
            // Show loading screen if requested
            if (_currentConfig.ShowLoadingScreen)
            {
                await UIService.ShowScreenAsync<LoadingScreen>();
                var loadingScreen = UIService.GetScreen<LoadingScreen>();
                
                // Set the loading type for context-specific messaging
                loadingScreen?.SetLoadingType(_currentConfig.Type);
            }
            
            try
            {
                switch (_currentConfig.Type)
                {
                    case LoadingType.NewGame:
                        await ProcessNewGameLoading();
                        break;
                        
                    case LoadingType.LoadSave:
                        await ProcessLoadSaveLoading();
                        break;
                        
                    case LoadingType.SceneTransition:
                        await ProcessSceneTransitionLoading();
                        break;
                        
                    case LoadingType.GameRestart:
                        await ProcessGameRestartLoading();
                        break;
                        
                    default:
                        Debug.LogError($"[LoadingState] Unknown loading type: {_currentConfig.Type}");
                        await TransitionToStateAsync(GameStateType.MainMenu);
                        return;
                }
                
                // Ensure minimum loading time for UX
                await EnsureMinimumLoadingTime();
                
                // Transition to playing state
                await TransitionToStateAsync(GameStateType.Playing);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadingState] Loading failed: {e}");
                await HandleLoadingFailure();
            }
        }
        
        private async Task ProcessNewGameLoading()
        {
        }
        
        private async Task ProcessLoadSaveLoading()
        {

        }
        
        private async Task ProcessSceneTransitionLoading()
        {
            await UpdateLoadingProgress("Transitioning...", 0.2f);
            
            // Load new scene
            await LoadScene(_currentConfig.SceneName);
            await UpdateLoadingProgress("Loading scene...", 0.8f);
            
            await UpdateLoadingProgress("Complete", 1.0f);
        }
        
        private async Task ProcessGameRestartLoading()
        { 
        }
        
        /// <summary>
        /// Better scene loading simulation (replace with actual scene loading)
        /// </summary>
        private async Task LoadScene(string sceneName)
        {
            // TODO: Replace with actual scene loading
            // await SceneManager.LoadSceneAsync(sceneName);
            
            // Simulate scene loading for now
            await Task.Delay(500);
            
            // Publish scene loaded event - TimeService will respond if needed
            EventSystem.Publish(new SceneLoadedEvent { SceneName = sceneName });
        }
        
        private async Task UpdateLoadingProgress(string message, float progress)
        {
            // Update loading screen if it exists
            var loadingScreen = UIService.GetScreen<LoadingScreen>();
            loadingScreen?.UpdateProgress(progress, message);
    
            // Publish single consolidated loading progress event
            EventSystem.Publish(new LoadingProgressEvent(message, progress));
    
            // Small delay for visual feedback
            await Task.Delay(100);
        }
        
        private async Task EnsureMinimumLoadingTime()
        {
            var elapsed = Time.time - _loadingStartTime;
            var remaining = _currentConfig.MinimumLoadingTime - elapsed;
            
            if (remaining > 0)
            {
                await Task.Delay((int)(remaining * 1000));
            }
        }
        
        private async Task HandleLoadingFailure()
        {
            Debug.LogError("[LoadingState] Loading failed, returning to main menu");
            
            // Show error message
            var loadingScreen = UIService.GetScreen<LoadingScreen>();
            loadingScreen?.ShowError("Loading failed. Returning to main menu...");
            
            await Task.Delay(2000); // Show error for 2 seconds
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        public override async Task ExitAsync()
        {
            // Hide loading screen
            if (_currentConfig?.ShowLoadingScreen == true)
            {
                await UIService.HideScreenAsync<LoadingScreen>();
            }
            
            // Clear loading configuration
            
            await base.ExitAsync();
        }
    }
}
