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
    /// NO LONGER HANDLES PAUSE STATE - use PauseService instead
    /// </summary>
    public class GameDataService : IGameDataService, IUpdatable
    {
        public bool IsInitialized { get; private set; }
        public GameSession CurrentSession { get; private set; }
        public LoadingConfiguration CurrentLoadingConfig { get; set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly ISaveService _saveService;
        
        // Auto-save timing
        private DateTime _lastAutoSaveCheck = DateTime.MinValue;
        private const int AUTO_SAVE_INTERVAL_MINUTES = 5;
        
        // Events
        public event Action<GameSession> OnSessionCreated;
        public event Action<GameSession> OnSessionLoaded;
        public event Action OnSessionCleared;

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
            
            IsInitialized = true;
            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            _eventSystem?.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
            
            ClearSession();
            CurrentLoadingConfig = null;
            IsInitialized = false;
        }

        public void Update()
        {
            // Always update session - PauseService will handle pause logic elsewhere
            UpdateSession();
        }
        
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
        /// Loads existing game session and adjusts session timing for continued playtime tracking
        /// </summary>
        public void LoadGameSession(GameSession session)
        {
            CurrentSession = session ?? throw new ArgumentNullException(nameof(session));
    
            // **CRITICAL FIX:** Adjust session start time to account for already accumulated playtime
            CurrentSession.AdjustSessionStartTimeForLoad();
    
            // Reset auto-save timer for loaded session
            _lastAutoSaveCheck = DateTime.Now;
    
            Debug.Log($"[GameDataService] Loaded game session for player '{CurrentSession.playerName}' with {CurrentSession.totalPlayTimeSeconds:F1}s playtime");
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
        /// Always updates - pause logic is handled by individual systems using PauseService
        /// Delegates actual saving to SaveService
        /// </summary>
        public void UpdateSession()
        {
            if (CurrentSession == null) return;
    
            // Always update play time - PauseService will handle pause logic elsewhere
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
        
        #endregion
    }
}
