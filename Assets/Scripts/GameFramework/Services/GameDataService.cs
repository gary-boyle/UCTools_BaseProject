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
    /// Clean GameDataService that manages unified GameSession data using EventSystem
    /// 
    /// Intent: Single source of truth for all game state information with event-driven architecture
    /// 
    /// Design:
    /// - Uses EventSystem for all communication instead of direct Action events
    /// - Delegates all save/load operations to SaveService
    /// - Uses TimeService for all playtime tracking - no manual time management
    /// - Publishes session lifecycle events through EventSystem
    /// 
    /// Pros:
    /// - Decoupled event handling through EventSystem
    /// - Consistent event architecture across the framework
    /// - Multiple systems can listen to session events without direct coupling
    /// - Easy to add new event listeners without modifying this service
    /// 
    /// Cons:
    /// - Slightly more overhead than direct Actions
    /// - Event handling requires knowledge of event class structure
    /// - Debugging event flow requires EventSystem awareness
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

        public GameDataService(IEventSystem eventSystem, ISaveService saveService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
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
            // Only handle auto-save timing - TimeService handles all playtime tracking
            UpdateSession();
        }
        
        #region GameSession Management
        
        /// <summary>
        /// Creates a new game session from loading configuration
        /// TimeService will handle playtime tracking automatically
        /// Publishes SessionCreatedEvent through EventSystem
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
                CurrentSession.SetCustomData(kvp.Key, kvp.Value);
            }
            
            // Reset auto-save timer for new session
            _lastAutoSaveCheck = DateTime.Now;
            
            // Publish session created event through EventSystem
            _eventSystem.Publish(new SessionCreatedEvent(CurrentSession));
        }
        
        /// <summary>
        /// Loads existing game session - TimeService handles playtime restoration
        /// Publishes SessionLoadedEvent through EventSystem
        /// </summary>
        public void LoadGameSession(GameSession session)
        {
            CurrentSession = session ?? throw new ArgumentNullException(nameof(session));
    
            // No manual time adjustment needed - TimeService handles all playtime tracking
            // TimeService will load playtime from the session's custom data automatically
    
            // Reset auto-save timer for loaded session
            _lastAutoSaveCheck = DateTime.Now;
            
            // Publish session loaded event through EventSystem
            _eventSystem.Publish(new SessionLoadedEvent(CurrentSession));
        }
        
        /// <summary>
        /// Clears the current session
        /// Publishes SessionClearedEvent through EventSystem
        /// </summary>
        public void ClearSession()
        {
            string playerName = null;
            
            if (CurrentSession != null)
            {
                playerName = CurrentSession.playerName;
            }
            
            CurrentSession = null;
            _lastAutoSaveCheck = DateTime.MinValue;
            
            // Publish session cleared event through EventSystem
            _eventSystem.Publish(new SessionClearedEvent(playerName));
        }
        
        /// <summary>
        /// Updates session and handles auto-save timing
        /// TimeService handles all playtime tracking - we just manage auto-saves
        /// </summary>
        public void UpdateSession()
        {
            if (CurrentSession == null) return;
    
            // No manual playtime updates needed - TimeService handles everything
            
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
            
            var result = await _saveService.PerformRegularSaveAsync();
            
            if (result.success)
            {
                Debug.Log($"[GameDataService] Manual save completed: {result.saveName} - " +
                         $"Playtime: {CurrentSession?.FormattedPlayTime ?? "N/A"}");
            }
            
            return result;
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
                Debug.Log($"[GameDataService] Auto-save completed: {result.saveName} - " +
                         $"Playtime: {CurrentSession?.FormattedPlayTime ?? "N/A"}");
            }
            else
            {
                Debug.LogWarning("[GameDataService] Auto-save failed");
            }
            
            return result;
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
            return CurrentSession.GetCustomData<T>(key, defaultValue) ?? defaultValue;
        }
        
        /// <summary>
        /// Sets custom data in current session
        /// </summary>
        public void SetCustomData<T>(string key, T value)
        {
            CurrentSession?.SetCustomData<T>(key, value);
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
            CurrentSession?.SetCurrentScene(evt.SceneName);
        }
        
        #endregion
    }
}
