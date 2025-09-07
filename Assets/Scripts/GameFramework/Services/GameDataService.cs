using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Clean GameDataService that manages unified GameSession data
    /// Single source of truth for all game state information
    /// Delegates all save/load operations to SaveService
    /// </summary>
    public class GameDataService : IGameDataService, IUpdatable
    {
        public bool IsInitialized { get; private set; }
        public GameSession CurrentSession { get; private set; }
        public LoadingConfiguration CurrentLoadingConfig { get; set; }
        
        // Global pause state - accessible by all systems
        public bool IsPaused { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly ISaveService _saveService;
        
        // Auto-save timing
        private DateTime _lastAutoSaveCheck = DateTime.MinValue;
        private const int AUTO_SAVE_INTERVAL_MINUTES = 5;
        
        // Events
        public event Action<GameSession> OnSessionCreated;
        public event Action<GameSession> OnSessionLoaded;
        public event Action OnSessionCleared;
        public event Action<bool> OnPauseStateChanged;

        public GameDataService(IEventSystem eventSystem, ISaveService saveService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[GameDataService] Initializing game data service...");
            
            // Subscribe to scene events to keep session updated
            _eventSystem.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
            
            // Subscribe to pause events to track global pause state
            _eventSystem.Subscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem.Subscribe<GameResumedEvent>(OnGameResumed);
            
            IsInitialized = true;
            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            _eventSystem?.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
            _eventSystem?.Unsubscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem?.Unsubscribe<GameResumedEvent>(OnGameResumed);
            
            ClearSession();
            CurrentLoadingConfig = null;
            IsPaused = false;
            IsInitialized = false;
        }

        public void Update()
        {
            // Only update session if game is not paused
            if (!IsPaused)
            {
                UpdateSession();
            }
        }
        
        #region Pause State Management
        
        /// <summary>
        /// Sets the global pause state
        /// </summary>
        public void SetPauseState(bool isPaused)
        {
            if (IsPaused != isPaused)
            {
                IsPaused = isPaused;
                OnPauseStateChanged?.Invoke(IsPaused);
                
                Debug.Log($"[GameDataService] Global pause state changed to: {IsPaused}");
            }
        }
        
        /// <summary>
        /// Checks if any game systems should pause their logic
        /// </summary>
        public bool ShouldPauseGameLogic()
        {
            return IsPaused;
        }
        
        #endregion
        
        #region GameSession Management
        
        /// <summary>
        /// Creates a new game session from loading configuration
        /// </summary>
        public void CreateNewGameSession(LoadingConfiguration config)
        {
            CurrentSession = GameSession.CreateNewGame(
                config.PlayerName, 
                config.GameData.ContainsKey("difficulty") ? config.GameData["difficulty"].ToString() : "Normal",
                config.SceneName
            );
            
            // Apply any custom data from loading config
            foreach (var kvp in config.GameData)
            {
                CurrentSession.customData[kvp.Key] = kvp.Value;
            }
            
            // Reset auto-save timer for new session
            _lastAutoSaveCheck = DateTime.Now;
            
            Debug.Log($"[GameDataService] Created new game session for player '{CurrentSession.playerName}'");
            OnSessionCreated?.Invoke(CurrentSession);
        }
        
        /// <summary>
        /// Loads existing game session
        /// </summary>
        public void LoadGameSession(GameSession session)
        {
            CurrentSession = session ?? throw new ArgumentNullException(nameof(session));
            
            // Reset auto-save timer for loaded session
            _lastAutoSaveCheck = DateTime.Now;
            
            Debug.Log($"[GameDataService] Loaded game session for player '{CurrentSession.playerName}'");
            OnSessionLoaded?.Invoke(CurrentSession);
        }
        
        /// <summary>
        /// Clears the current session
        /// </summary>
        public void ClearSession()
        {
            CurrentSession = null;
            _lastAutoSaveCheck = DateTime.MinValue;
            OnSessionCleared?.Invoke();
        }
        
        /// <summary>
        /// Updates current session with play time and handles auto-save timing
        /// Only called when not paused - delegates actual saving to SaveService
        /// </summary>
        public void UpdateSession()
        {
            if (CurrentSession == null) return;
            
            // Update play time
            CurrentSession.UpdatePlayTime();
            
            // Check if it's time for an auto-save
            var timeSinceLastCheck = DateTime.Now - _lastAutoSaveCheck;
            if (timeSinceLastCheck.TotalMinutes >= AUTO_SAVE_INTERVAL_MINUTES)
            {
                _lastAutoSaveCheck = DateTime.Now;
                
                // Delegate to SaveService's auto-save logic
                _ = PerformAutoSaveAsync();
            }
        }
        
        /// <summary>
        /// Performs a regular save using SaveService - returns save name for UI feedback
        /// </summary>
        public async Task<(bool success, string saveName)> SaveCurrentSessionAsync()
        {
            if (!_saveService.CanSaveGame())
            {
                Debug.LogWarning("[GameDataService] Cannot save game - no active session or save service unavailable");
                return (false, null);
            }
            
            return await _saveService.PerformRegularSaveAsync();
        }
        
        /// <summary>
        /// Performs an auto-save using SaveService
        /// </summary>
        public async Task<(bool success, string saveName)> PerformAutoSaveAsync()
        {
            if (!_saveService.CanSaveGame())
            {
                Debug.LogWarning("[GameDataService] Cannot auto-save - no active session or save service unavailable");
                return (false, null);
            }
            
            var result = await _saveService.PerformAutoSaveAsync();
            
            if (result.success)
            {
                Debug.Log($"[GameDataService] Auto-save completed: {result.saveName}");
            }
            else
            {
                Debug.LogWarning("[GameDataService] Auto-save failed");
            }
            
            return result;
        }
        
        /// <summary>
        /// Loads a session from save file using SaveService
        /// </summary>
        public async Task<bool> LoadSessionAsync(string saveName)
        {
            var session = await _saveService.LoadGameSessionAsync(saveName);
            if (session != null)
            {
                LoadGameSession(session);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Loads a session using SaveFileInfo from SaveService
        /// </summary>
        public async Task<bool> LoadSessionAsync(SaveFileInfo saveFileInfo)
        {
            if (saveFileInfo == null) return false;
            
            var session = await _saveService.LoadGameSessionByInfoAsync(saveFileInfo);
            if (session != null)
            {
                LoadGameSession(session);
                return true;
            }
            return false;
        }
        
        #endregion

        #region Data Access Convenience Methods
        
        /// <summary>
        /// Gets the current player state - throws if no active session
        /// </summary>
        public PlayerState GetPlayerState() 
        {
            if (CurrentSession?.player == null)
                throw new InvalidOperationException("No active game session or player state");
            return CurrentSession.player;
        }
        
        /// <summary>
        /// Gets the current game progress - throws if no active session  
        /// </summary>
        public GameProgress GetGameProgress() 
        {
            if (CurrentSession?.progress == null)
                throw new InvalidOperationException("No active game session or progress data");
            return CurrentSession.progress;
        }
        
        /// <summary>
        /// Gets custom data from current session
        /// </summary>
        public T GetCustomData<T>(string key, T defaultValue = default) 
        {
            if (CurrentSession?.customData.ContainsKey(key) == true)
            {
                try { return (T)CurrentSession.customData[key]; }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Sets custom data in current session
        /// </summary>
        public void SetCustomData<T>(string key, T value)
        {
            if (CurrentSession != null)
            {
                CurrentSession.customData[key] = value;
            }
        }
        
        /// <summary>
        /// Checks if there's an active game session
        /// </summary>
        public bool HasActiveSession() => CurrentSession != null;
        
        /// <summary>
        /// Gets loading configuration data
        /// </summary>
        public T GetLoadingData<T>(string key, T defaultValue = default)
        {
            if (CurrentLoadingConfig?.GameData?.ContainsKey(key) == true)
            {
                try { return (T)CurrentLoadingConfig.GameData[key]; }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
        
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// Updates current scene when scene loads
        /// </summary>
        private void OnSceneLoaded(SceneLoadedEvent evt)
        {
            if (CurrentSession != null)
            {
                CurrentSession.currentScene = evt.SceneName;
            }
        }
        
        /// <summary>
        /// Handles global pause events
        /// </summary>
        private void OnGamePaused(GamePausedEvent evt)
        {
            SetPauseState(true);
        }
        
        /// <summary>
        /// Handles global resume events
        /// </summary>
        private void OnGameResumed(GameResumedEvent evt)
        {
            SetPauseState(false);
        }
        
        #endregion
    }
}
