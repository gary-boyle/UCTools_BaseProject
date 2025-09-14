using System;
using System.Collections.Generic;
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
            string difficulty = "Normal";
            
            // Extract difficulty from config if available
            if (config.GameData.ContainsKey("difficulty"))
            {
                difficulty = config.GameData["difficulty"].ToString();
            }
            
            CurrentSession = GameSession.CreateNewGame(
                config.PlayerName, 
                difficulty,
                config.SceneName
            );
            
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
            // TimeService will load playtime from the session's time data automatically
    
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
        /// Performs an auto-save using SaveService
        /// </summary>
        public async Task<bool> PerformAutoSaveAsync()
        {
            if (!_saveService.CanSaveGame())
            {
                Debug.LogWarning("[GameDataService] Cannot auto-save - no active session or save service unavailable");
                return false;
            }
            
            _eventSystem.Publish(SaveRequestedEvent.CreateAutoSave());
            
            return true;
        }
        
        #endregion

        #region GameSession Validation

        /// <summary>
        /// Validates the integrity and completeness of a GameSession
        /// Checks all critical fields and data structures for consistency
        /// </summary>
        /// <param name="session">The GameSession to validate</param>
        /// <returns>True if the session is valid and can be safely loaded</returns>
        public bool IsValidGameSession(GameSession session)
        {
            if (session == null)
            {
                Debug.LogError("[GameDataService] GameSession is null");
                return false;
            }

            // Validate required string fields
            if (string.IsNullOrEmpty(session.playerName))
            {
                Debug.LogError("[GameDataService] GameSession has invalid player name");
                return false;
            }

            if (string.IsNullOrEmpty(session.currentScene))
            {
                Debug.LogError("[GameDataService] GameSession has invalid current scene");
                return false;
            }

            // Validate player state
            if (session.player == null)
            {
                Debug.LogError("[GameDataService] GameSession has null player state");
                return false;
            }

            if (session.player.MaxHealth <= 0)
            {
                Debug.LogError("[GameDataService] GameSession has invalid player max health");
                return false;
            }

            if (session.player.Health > session.player.MaxHealth)
            {
                Debug.LogWarning("[GameDataService] GameSession player health exceeds max health - auto-correcting");
                session.player.Health = session.player.MaxHealth;
            }

            if (session.player.Level < 1)
            {
                Debug.LogError("[GameDataService] GameSession has invalid player level");
                return false;
            }

            // Validate progress data
            if (session.progress == null)
            {
                Debug.LogError("[GameDataService] GameSession has null progress data");
                return false;
            }

            if (session.progress.Score < 0)
            {
                Debug.LogWarning("[GameDataService] GameSession has negative score - resetting to 0");
                session.progress.Score = 0;
            }

            // Validate timestamps
            if (session.sessionStartTime == default(DateTime))
            {
                Debug.LogWarning("[GameDataService] GameSession has invalid start time - using current time");
                session.sessionStartTime = DateTime.Now;
            }

            if (session.lastSaveTime == default(DateTime))
            {
                Debug.LogWarning("[GameDataService] GameSession has invalid save time - using current time");
                session.lastSaveTime = DateTime.Now;
            }

            // Validate playtime data (non-negative)
            if (session.TotalPlayTimeSeconds < 0)
            {
                Debug.LogError("[GameDataService] GameSession has negative total playtime");
                return false;
            }

            Debug.Log($"[GameDataService] GameSession validation passed: {session.playerName} - {session.FormattedPlayTime}");
            return true;
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
