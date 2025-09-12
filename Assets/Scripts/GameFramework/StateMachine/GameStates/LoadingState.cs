using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.DataStructures;
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

            _currentConfig = GameDataService.CurrentLoadingConfig;
            _loadingStartTime = Time.time;
            
            if (_currentConfig == null)
            {
                Debug.LogError("[LoadingState] No loading configuration provided!");
                await TransitionToStateAsync(GameStateType.MainMenu);
                return;
            }
            
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
        }
        
        private async Task ProcessLoadSaveLoading()
        {
            await UpdateLoadingProgress("Loading save data...", 0.1f);
            
            // Get the GameSession directly from loading config
            GameSession session = null;
            if (_currentConfig.GameData.ContainsKey("gameSession"))
            {
                session = (GameSession)_currentConfig.GameData["gameSession"];
            }
            else
            {
                // Fallback: create from data if session not directly stored
                session = CreateSessionFromSaveData(_currentConfig);
            }
            
            if (session == null)
            {
                throw new InvalidOperationException("Could not load game session from save data");
            }
            
            // Load the session into GameDataService - TimeService will handle playtime restoration
            GameDataService.LoadGameSession(session);
            await UpdateLoadingProgress("Restoring game state...", 0.4f);
            
            // Load the appropriate scene
            await LoadScene(_currentConfig.SceneName);
            await UpdateLoadingProgress("Loading world...", 0.7f);
            
            // Initialize game systems with loaded data
            await InitializeGameSystems();
            await UpdateLoadingProgress("Finalizing...", 1.0f);
        }
        
        private async Task ProcessSceneTransitionLoading()
        {
            await UpdateLoadingProgress("Transitioning...", 0.2f);
            
            // Update current session's scene
            if (GameDataService.HasActiveSession())
            {
                GameDataService.CurrentSession.SetCurrentScene(_currentConfig.SceneName);
                
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
        }
        
        private async Task ProcessGameRestartLoading()
        { 
            await UpdateLoadingProgress("Restarting...", 0.1f);
            
            // Clear current session and create new one - TimeService will reset timers
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
        
        /// <summary>
        /// Creates a GameSession from save data stored in loading configuration
        /// TimeService will handle playtime restoration automatically
        /// </summary>
        private static GameSession CreateSessionFromSaveData(LoadingConfiguration config)
        {
            // Extract saved session data from loading configuration
            var session = new GameSession
            {
                playerName = config.PlayerName,
                difficulty = config.GameData.ContainsKey("difficulty") ? config.GameData["difficulty"].ToString() : "Normal",
                currentScene = config.SceneName,
                sessionStartTime = config.GameData.ContainsKey("sessionStartTime") ? 
                    DateTime.Parse(config.GameData["sessionStartTime"].ToString()) : DateTime.Now,
                lastSaveTime = config.GameData.ContainsKey("lastSaveTime") ? 
                    DateTime.Parse(config.GameData["lastSaveTime"].ToString()) : DateTime.Now,
                customData = new System.Collections.Generic.Dictionary<string, object>(config.GameData)
            };
            
            // Restore player state from save data
            session.player = new PlayerState
            {
                Level = config.GameData.ContainsKey("playerLevel") ? Convert.ToInt32(config.GameData["playerLevel"]) : 1,
                Health = config.GameData.ContainsKey("playerHealth") ? Convert.ToInt32(config.GameData["playerHealth"]) : 100,
                MaxHealth = config.GameData.ContainsKey("playerMaxHealth") ? Convert.ToInt32(config.GameData["playerMaxHealth"]) : 100,
                Experience = config.GameData.ContainsKey("playerExperience") ? Convert.ToSingle(config.GameData["playerExperience"]) : 0f,
                Position = config.GameData.ContainsKey("playerPosition") ? 
                    (Vector3)config.GameData["playerPosition"] : Vector3.zero
            };
            
            // Restore progress state from save data
            session.progress = new GameProgress
            {
                Score = config.GameData.ContainsKey("score") ? Convert.ToInt32(config.GameData["score"]) : 0
            };
            
            // Store TimeService-related data for restoration
            if (config.GameData.ContainsKey("savedPlayTime"))
            {
                session.SetCustomData("GameTime", Convert.ToSingle(config.GameData["savedPlayTime"]));
            }
            
            return session;
        }
        
        private async Task InitializeGameSystems()
        {
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
            GameDataService.CurrentLoadingConfig = null;
            _currentConfig = null;
            
            await base.ExitAsync();
        }
    }
}
