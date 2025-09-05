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
    /// Loading state that handles different loading scenarios using unified GameSession system
    /// Creates or loads GameSession based on loading configuration
    /// </summary>
    public class LoadingState : BaseGameState
    {
        protected readonly IGameDataService GameDataService;

        private LoadingConfiguration _currentConfig;
        private float _loadingStartTime;
        
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
            
            await UpdateLoadingProgress("Initializing new game...", 0.1f);
            
            // Create new game session from loading configuration
            GameDataService.CreateNewGameSession(_currentConfig);
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
            
            // Create GameSession from save data in loading configuration
            var session = CreateSessionFromSaveData(_currentConfig);
            GameDataService.LoadGameSession(session);
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
            
            // Update current session's scene
            if (GameDataService.HasActiveSession())
            {
                GameDataService.CurrentSession.currentScene = _currentConfig.SceneName;
                
                // Apply any transition data to current session
                foreach (var kvp in _currentConfig.GameData)
                {
                    GameDataService.SetCustomData(kvp.Key, kvp.Value);
                }
            }
            
            // Load new scene
            await LoadScene(_currentConfig.SceneName);
            await UpdateLoadingProgress("Loading scene...", 0.8f);
            
            await UpdateLoadingProgress("Complete", 1.0f);
            
            Debug.Log("[LoadingState] Scene transition complete");
        }
        
        private async Task ProcessGameRestartLoading()
        {
            Debug.Log("[LoadingState] Processing game restart...");
            
            await UpdateLoadingProgress("Restarting...", 0.1f);
            
            // Clear current session and create new one
            GameDataService.ClearSession();
            await UpdateLoadingProgress("Resetting...", 0.4f);
            
            // Create fresh session from current config
            GameDataService.CreateNewGameSession(_currentConfig);
            
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
        
        /// <summary>
        /// Creates a GameSession from save data stored in loading configuration
        /// </summary>
        private GameFramework.DataStructures.GameSession CreateSessionFromSaveData(LoadingConfiguration config)
        {
            // Extract saved session data from loading configuration
            var session = new GameFramework.DataStructures.GameSession
            {
                playerName = config.PlayerName,
                difficulty = config.GameData.ContainsKey("difficulty") ? config.GameData["difficulty"].ToString() : "Normal",
                currentScene = config.SceneName,
                sessionStartTime = config.GameData.ContainsKey("sessionStartTime") ? 
                    DateTime.Parse(config.GameData["sessionStartTime"].ToString()) : DateTime.Now,
                totalPlayTimeSeconds = config.GameData.ContainsKey("totalPlayTime") ? 
                    Convert.ToSingle(config.GameData["totalPlayTime"]) : 0f,
                customData = new System.Collections.Generic.Dictionary<string, object>(config.GameData)
            };
            
            // Restore player state from save data
            session.player = new GameFramework.DataStructures.PlayerState
            {
                level = config.GameData.ContainsKey("playerLevel") ? Convert.ToInt32(config.GameData["playerLevel"]) : 1,
                health = config.GameData.ContainsKey("playerHealth") ? Convert.ToInt32(config.GameData["playerHealth"]) : 100,
                maxHealth = config.GameData.ContainsKey("playerMaxHealth") ? Convert.ToInt32(config.GameData["playerMaxHealth"]) : 100,
                experience = config.GameData.ContainsKey("playerExperience") ? Convert.ToSingle(config.GameData["playerExperience"]) : 0f,
                position = config.GameData.ContainsKey("playerPosition") ? 
                    (UnityEngine.Vector3)config.GameData["playerPosition"] : UnityEngine.Vector3.zero
            };
            
            // Restore progress state from save data
            session.progress = new GameFramework.DataStructures.GameProgress
            {
                score = config.GameData.ContainsKey("score") ? Convert.ToInt32(config.GameData["score"]) : 0
            };
            
            return session;
        }
        
        private async Task InitializeGameSystems()
        {
            Debug.Log("[LoadingState] Initializing game systems...");
            
            // Initialize or refresh game systems based on current session
            var session = GameDataService.CurrentSession;
            if (session != null)
            {
                Debug.Log($"[LoadingState] Initializing for player '{session.playerName}' at level {session.player.level}");
            }
            
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
