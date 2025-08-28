using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Loading state that handles different loading scenarios based on configuration
    /// </summary>
    public class LoadingState : BaseGameState
    {
        protected readonly IGameDataService GameDataService;

        private LoadingConfiguration _currentConfig;
        private float _loadingStartTime;
        
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public LoadingState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.Loading, stateMachine, eventSystem, audioService, uiService, inputService, consoleService, gameDataService)
        {
            GameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            _currentConfig = GameDataService.CurrentLoadingConfig;
            _loadingStartTime = Time.time;
            
            if (_currentConfig == null)
            {
                Debug.LogError("[LoadingState] No loading configuration provided!");
                await TransitionToStateAsync(GameStateType.MainMenu);
                return;
            }
            
            Debug.Log($"[LoadingState] Starting {_currentConfig.Type} loading process");
            
            // Show loading screen if requested
            if (_currentConfig.ShowLoadingScreen)
            {
                await UIService.ShowScreenAsync<LoadingScreen>();
                var loadingScreen = UIService.GetScreen<LoadingScreen>();
                //loadingScreen?.SetLoadingType(_currentConfig.Type);
            }
            
            // Start the appropriate loading process
            await ProcessLoadingConfiguration();
        }
        
        private async Task ProcessLoadingConfiguration()
        {
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
            Debug.Log("[LoadingState] Processing new game loading...");
            
            // Update loading progress
            await UpdateLoadingProgress("Initializing new game...", 0.1f);
            
            // Initialize player data
            await InitializeNewGameData();
            await UpdateLoadingProgress("Setting up player...", 0.3f);
            
            // Load the scene
            await LoadScene(_currentConfig.SceneName);
            await UpdateLoadingProgress("Loading world...", 0.7f);
            
            // Initialize game systems for new game
            await InitializeGameSystems();
            await UpdateLoadingProgress("Finalizing...", 1.0f);
            
            Debug.Log("[LoadingState] New game loading complete");
        }
        
        private async Task ProcessLoadSaveLoading()
        {
            Debug.Log("[LoadingState] Processing saved game loading...");
            
            await UpdateLoadingProgress("Loading save data...", 0.1f);
            
            // Apply saved data to game systems
            await ApplySaveData();
            await UpdateLoadingProgress("Restoring game state...", 0.4f);
            
            // Load the appropriate scene
            await LoadScene(_currentConfig.SceneName);
            await UpdateLoadingProgress("Loading world...", 0.7f);
            
            // Initialize game systems with saved data
            await InitializeGameSystems();
            await UpdateLoadingProgress("Finalizing...", 1.0f);
            
            Debug.Log("[LoadingState] Save game loading complete");
        }
        
        private async Task ProcessSceneTransitionLoading()
        {
            Debug.Log("[LoadingState] Processing scene transition...");
            
            await UpdateLoadingProgress("Transitioning...", 0.2f);
            
            // Load new scene
            await LoadScene(_currentConfig.SceneName);
            await UpdateLoadingProgress("Loading scene...", 0.8f);
            
            // Apply any transition data
            await ApplyTransitionData();
            await UpdateLoadingProgress("Complete", 1.0f);
            
            Debug.Log("[LoadingState] Scene transition complete");
        }
        
        private async Task ProcessGameRestartLoading()
        {
            Debug.Log("[LoadingState] Processing game restart...");
            
            await UpdateLoadingProgress("Restarting...", 0.1f);
            
            // Clear any existing game state
            await ClearGameState();
            await UpdateLoadingProgress("Resetting...", 0.4f);
            
            // Reload the scene
            await LoadScene(_currentConfig.SceneName);
            await UpdateLoadingProgress("Reloading...", 0.8f);
            
            // Initialize fresh game systems
            await InitializeGameSystems();
            await UpdateLoadingProgress("Complete", 1.0f);
            
            Debug.Log("[LoadingState] Game restart complete");
        }
        
        private async Task LoadScene(string sceneName)
        {
            Debug.Log($"[LoadingState] Loading scene: {sceneName}");
            
            // Use the scene service to load the scene
            //await AudioService.PlaySFXAsync("scene_transition");
            
            // Assuming your SceneService has an async load method
            // You'll need to implement this in your SceneService
            // await SceneService.LoadSceneAsync(sceneName);
            
            // For now, simulate scene loading
            await Task.Delay(1000); // Simulate scene load time
            
            Debug.Log($"[LoadingState] Scene {sceneName} loaded successfully");
        }
        
        private async Task InitializeNewGameData()
        {
            var config = GameDataService.CurrentLoadingConfig;
            
            // Set up player data from configuration
            GameDataService.PlayerName = config.PlayerName;
            //GameDataService.IsNewGame = config.GetLoadingData("isNewGame", true);
            //GameDataService.PlayerLevel = config.GetLoadingData("playerLevel", 1);
            
            // Set other values
            //GameDataService.SetValue("spawnPoint", config.GetLoadingData("startingPosition", "DefaultSpawn"));
            //GameDataService.SetValue("difficulty", config.GetLoadingData("difficulty", "Normal"));
            
            // Set session start time
            GameDataService.SessionStartTime = DateTime.Now;
        }
        
        private async Task ApplySaveData()
        {
            var config = GameDataService.CurrentLoadingConfig;
            
            // Apply saved game data
            GameDataService.SetValues(config.GameData);
            GameDataService.IsNewGame = false;
        }
        
        private async Task ApplyTransitionData()
        {
            // Apply any scene transition data
            if (_currentConfig.GameData?.Count > 0)
            {
                foreach (var kvp in _currentConfig.GameData)
                {
                    GameDataService.SetValue(kvp.Key, kvp.Value);
                }
            }
            
            await Task.CompletedTask;
        }
        
        private async Task ClearGameState()
        {
            // Clear any persistent game state for restart
            GameDataService.ClearTransientData();
            await Task.CompletedTask;
        }
        
        private async Task InitializeGameSystems()
        {
            Debug.Log("[LoadingState] Initializing game systems...");
            
            // Initialize or refresh game systems based on loaded data
            // This is where you'd set up player controllers, world state, etc.
            
            // Publish initialization events
            EventSystem.Publish(new GameSystemsInitializedEvent
            {
                LoadingType = _currentConfig.Type,
                GameData = _currentConfig.GameData
            });
            
            await Task.CompletedTask;
        }
        
        private async Task UpdateLoadingProgress(string message, float progress)
        {
            Debug.Log($"[LoadingState] {message} ({progress:P0})");
            
            // Update loading screen if it exists
            var loadingScreen = UIService.GetScreen<LoadingScreen>();
            //loadingScreen?.UpdateProgress(progress, message);
            
            // Publish loading progress event
            EventSystem.Publish(new LoadingProgressEvent
            {
                Progress = progress,
                Message = message
            });
            
            // Small delay for visual feedback
            await Task.Delay(100);
        }
        
        private async Task EnsureMinimumLoadingTime()
        {
            var elapsed = Time.time - _loadingStartTime;
            var remaining = _currentConfig.MinimumLoadingTime - elapsed;
            
            if (remaining > 0)
            {
                Debug.Log($"[LoadingState] Waiting {remaining:F1}s for minimum loading time");
                await Task.Delay((int)(remaining * 1000));
            }
        }
        
        private async Task HandleLoadingFailure()
        {
            Debug.LogError("[LoadingState] Loading failed, returning to main menu");
            
            // Show error message
            var loadingScreen = UIService.GetScreen<LoadingScreen>();
            //loadingScreen?.ShowError("Loading failed. Returning to main menu...");
            
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
            GameDataService.CurrentLoadingConfig = null;
            _currentConfig = null;
            
            await base.ExitAsync();
        }
    }
}
