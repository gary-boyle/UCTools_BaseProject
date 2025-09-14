using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Core;
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
    /// GameDataService manages GameSession lifecycle and provides session creation utilities
    /// 
    /// Intent: Single source of truth for game state with session creation and management
    /// 
    /// Design:
    /// - Handles all GameSession creation logic (moved from GameSession)
    /// - Uses TimeService for all time-related operations
    /// - Manages session lifecycle and save timing
    /// - Uses EventSystem for communication
    /// 
    /// Pros: Clear separation of concerns, centralized session management
    /// Cons: Slightly more complex but better architecture
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

        public GameDataService(
            IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
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
        
        #region GameSession Creation (Moved from GameSession)
        
        /// <summary>
        /// Creates a new game session from loading configuration (moved from GameSession)
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
            
            CurrentSession = CreateNewGameSession(
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
        /// Creates a new GameSession with specified parameters (moved from GameSession static method)
        /// </summary>
        public GameSession CreateNewGameSession(string playerName, string difficulty, string startingScene)
        {
            var now = DateTime.Now;
            var session = new GameSession
            {
                PlayerName = playerName,
                Difficulty = difficulty,
                CurrentScene = startingScene,
                SessionStartTime = now,
                LastSaveTime = now,
                WasAutoSave = false
            };
            
            // Initialize time data
            session.SetSavedTimeData(0f);
            session.SetHasTimeData(true);
            
            Debug.Log($"[GameDataService] Created new game session: {session}");
            return session;
        }
        
        /// <summary>
        /// Updates the last save time and captures current playtime data (moved from GameSession)
        /// Called when the game is saved
        /// </summary>
        public void UpdateSessionSaveTime(GameSession session = null)
        {
            var targetSession = session ?? CurrentSession;
            if (targetSession == null)
            {
                Debug.LogWarning("[GameDataService] Cannot update save time - no session provided");
                return;
            }
            
            targetSession.LastSaveTime = DateTime.Now;

            var timeService = GameManager.GetService<ITimeService>();
            // Use TimeService to update the session's time data
            if (timeService.IsInitialized)
            {
                timeService.UpdateSessionTimeData(targetSession);
            }
        }
        
        #endregion
        
        #region GameSession Management
        
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
                playerName = CurrentSession.PlayerName;
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
            if (CurrentSession != null)
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
            if (string.IsNullOrEmpty(session.PlayerName))
            {
                Debug.LogError("[GameDataService] GameSession has invalid player name");
                return false;
            }

            if (string.IsNullOrEmpty(session.CurrentScene))
            {
                Debug.LogError("[GameDataService] GameSession has invalid current scene");
                return false;
            }

            // Validate timestamps
            if (session.SessionStartTime == default(DateTime))
            {
                Debug.LogWarning("[GameDataService] GameSession has invalid start time - using current time");
                session.SessionStartTime = DateTime.Now;
            }

            if (session.LastSaveTime == default(DateTime))
            {
                Debug.LogWarning("[GameDataService] GameSession has invalid save time - using current time");
                session.LastSaveTime = DateTime.Now;
            }

            // Validate playtime data (non-negative)
            if (session.SavedGameTime < 0)
            {
                Debug.LogError("[GameDataService] GameSession has negative saved game time");
                return false;
            }
            return true;
        }
        
        #endregion

        #region Data Access Convenience Methods
        
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
