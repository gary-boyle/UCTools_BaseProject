using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.Input;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.UI.Screens;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Quit state - handles graceful application shutdown with progress feedback
    /// Responsible for cleaning up resources, and exiting the application
    /// </summary>
    public class QuitState : BaseGameState
    {
        private QuitScreen _quitScreen;
        private bool _shutdownCancelled = false;
        private bool _criticalShutdownPhase = false;

        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public QuitState(
            GameContext context,
            IGameStateMachine stateMachine)
            : base(GameStateType.Quit, context, stateMachine)
        {
        }
        
        /// <summary>
        /// Enter quit state - begin graceful shutdown process
        /// </summary>
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            Debug.Log("[QuitState] Entering Quit state - Beginning graceful shutdown");
            
            // Set input context for UI (in case user wants to cancel)
            InputManager.SetInputContext(InputContext.UI);
            
            // Subscribe to cancellation events
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnShutdownCancelled);
            
            // Show quit screen with progress
            await UIService.ShowScreenAsync<QuitScreen>();
            
            // Start the shutdown process
            await BeginShutdownProcess();
        }
        
        /// <summary>
        /// Exit quit state - cleanup if shutdown was cancelled
        /// </summary>
        public override async Task ExitAsync()
        {
            Debug.Log("[QuitState] Exiting Quit state");
            
            // Unsubscribe from events
            EventSystem.Unsubscribe<MainMenuRequestedEvent>(OnShutdownCancelled);
            
            // Hide quit screen if still visible
            await UIService.HideScreenAsync<QuitScreen>();
            
            await base.ExitAsync();
        }
        
        #region Shutdown Process
        
        /// <summary>
        /// Begin the graceful shutdown process with progress tracking
        /// </summary>
        private async Task BeginShutdownProcess()
        {
            try
            {
                _shutdownCancelled = false;
                
                // Phase 1: Prepare for shutdown (0-20%)
                await ExecuteShutdownPhase("Preparing for shutdown...", 0.0f, 0.2f, PrepareForShutdown);
                if (_shutdownCancelled) return;
                
                // Phase 3: Clean up resources (50-80%)
                _criticalShutdownPhase = true; // No cancellation after this point
                _quitScreen?.SetShuttingDown(true);
                await ExecuteShutdownPhase("Cleaning up resources...", 0.5f, 0.8f, CleanupResources);
                
                // Phase 4: Final shutdown (80-100%)
                await ExecuteShutdownPhase("Finalizing shutdown...", 0.8f, 1.0f, FinalizeShutdown);
                
                // Complete shutdown
                _quitScreen?.MarkShutdownComplete();
                await Task.Delay(1000); // Brief pause to show completion
                
                // Actually quit the application
                QuitApplication();
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuitState] Error during shutdown process: {e.Message}");
                
                // On error, still attempt to quit gracefully
                _quitScreen?.UpdateProgress(1.0f, "Shutdown encountered an error, forcing quit...", false);
                await Task.Delay(2000);
                QuitApplication();
            }
        }
        
        /// <summary>
        /// Execute a shutdown phase with progress tracking
        /// </summary>
        private async Task ExecuteShutdownPhase(string actionName, float startProgress, float endProgress, Func<Task> phaseAction)
        {
            _quitScreen?.UpdateProgress(startProgress, actionName, !_criticalShutdownPhase);
            
            await phaseAction();
            
            // Animate progress to end of phase
            await AnimateProgressToTarget(endProgress, actionName);
        }
        
        /// <summary>
        /// Animate progress bar to target value
        /// </summary>
        private async Task AnimateProgressToTarget(float targetProgress, string actionName)
        {
            float startProgress = 0f;
            float duration = 0.5f; 
            float elapsed = 0f;
            
            while (elapsed < duration && !_shutdownCancelled)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float animatedProgress = Mathf.Lerp(startProgress, targetProgress, t);
                
                _quitScreen?.UpdateProgress(animatedProgress, actionName, !_criticalShutdownPhase);
                
                await Task.Yield();
            }
            
            // Ensure we end at the target
            if (!_shutdownCancelled)
            {
                _quitScreen?.UpdateProgress(targetProgress, actionName, !_criticalShutdownPhase);
            }
        }
        
        #endregion
        
        #region Shutdown Phases
        
        /// <summary>
        /// Phase 1: Prepare for shutdown
        /// </summary>
        private async Task PrepareForShutdown()
        {
            // Pause any ongoing game processes
            if (GameDataService.HasActiveSession())
            {
                // Could pause timers, stop gameplay, etc.
            }
            
            // Stop any background processes
            //AudioService.StopMusic();
            
            await Task.Delay(500); // Simulate preparation time
        }

        /// <summary>
        /// Phase 3: Clean up resources
        /// </summary>
        private async Task CleanupResources()
        {
            try
            {
                // Close all UI screens and popups
                await UIService.CloseAllPopupsAsync();
                
                // Shutdown services
                ConsoleService.Shutdown();
                Context.AudioService?.Shutdown();
                
                // Cleanup any other resources
                await Task.Delay(800); // Simulate cleanup time
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[QuitState] Error during resource cleanup: {e.Message}");
            }
        }
        
        /// <summary>
        /// Phase 4: Finalize shutdown
        /// </summary>
        private async Task FinalizeShutdown()
        {
            // Any final cleanup tasks
            EventSystem?.Clear();
            
            await Task.Delay(500); // Final pause
        }
        
        /// <summary>
        /// Actually quit the application
        /// </summary>
        private static void QuitApplication()
        {
#if UNITY_EDITOR
            // In editor, stop play mode
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // In build, quit application
            Application.Quit();
#endif
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle shutdown cancellation request
        /// </summary>
        private async void OnShutdownCancelled(MainMenuRequestedEvent evt)
        {
            if (!_criticalShutdownPhase)
            {
                _shutdownCancelled = true;
                
                // Return to main menu
                await TransitionToStateAsync(GameStateType.MainMenu);
            }
            else
            {
                Debug.Log("[QuitState] Cannot cancel shutdown - in critical phase");
            }
        }
        
        #endregion
    }
}
