using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Data;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.DataStructures;
using GameFramework.Utilities;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// TimeService manages game time tracking and provides time-related utilities
    /// Handles time formatting, playtime info generation, and session time synchronization
    /// 
    /// Design: Centralized time management with formatting utilities for GameSession data
    /// Pros: All time logic in one place, consistent formatting, proper session integration
    /// Cons: Slightly more complex interface but better separation of concerns
    /// </summary>
    public class TimeService : ITimeService, IUpdatable
    {
        #region Properties and Fields
        
        public bool IsInitialized { get; private set; }
        
        // Time tracking properties
        public float GameTime => _gameTime;
        public float SessionTime => _sessionTime;
        
        // State tracking
        public bool IsTrackingGameTime => _isInPlayingState && !_isPaused;
        public bool IsTrackingSessionTime => _isInitialized && !_isPaused;
        
        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly IGameDataService _gameDataService;
        
        // Internal time tracking
        private float _gameTime = 0f;          // Time spent actually playing (PlayingState + not paused)
        private float _sessionTime = 0f;       // Total time since service started (excluding pause)
        private float _levelTime = 0f;         // Time spent in current level/scene
        
        // State flags
        private bool _isInPlayingState = false;
        private bool _isPaused = false;
        private bool _isInitialized = false;
        
        // For delta time calculations
        private float _lastUpdateTime;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public TimeService(
            IEventSystem eventSystem, 
            IGameDataService gameDataService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }
        
        #endregion
        
        #region Initialization and Shutdown
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            // Subscribe to game state changes
            _eventSystem.Subscribe<GameStateChangeEvent>(OnGameStateChanged);
            
            // Subscribe to pause events
            _eventSystem.Subscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem.Subscribe<GameResumedEvent>(OnGameResumed);
            
            // Subscribe to game lifecycle events
            _eventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            _eventSystem.Subscribe<SessionLoadedEvent>(OnSessionLoaded);
            _eventSystem.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
            
            // Initialize time tracking
            _lastUpdateTime = Time.realtimeSinceStartup;
            _isInitialized = true;
            
            // Load existing time data if we have an active session
            LoadTimeDataFromSession();
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            // Unsubscribe from events
            _eventSystem?.Unsubscribe<GameStateChangeEvent>(OnGameStateChanged);
            _eventSystem?.Unsubscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem?.Unsubscribe<GameResumedEvent>(OnGameResumed);
            _eventSystem?.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            _eventSystem?.Unsubscribe<SessionLoadedEvent>(OnSessionLoaded);
            _eventSystem?.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
            
            _isInitialized = false;
            IsInitialized = false;
        }
        
        #endregion
        
        #region Update Loop
        
        /// <summary>
        /// Update time tracking - called every frame by GameManager
        /// </summary>
        public void Update()
        {
            if (!_isInitialized) return;
    
            // Calculate delta time using real time (unaffected by Time.timeScale)
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - _lastUpdateTime;
            _lastUpdateTime = currentTime;

            // Update timers based on current state
            if (!_isPaused)
            {
                // Always update session time when not paused
                _sessionTime += deltaTime;
        
                // Only update game time when in playing state
                if (_isInPlayingState)
                {
                    _gameTime += deltaTime;
                }
        
                // Always update level time when not paused (could be refined based on needs)
                _levelTime += deltaTime;
            }
        }

        #endregion
        
        #region Time Formatting Utilities (Moved from GameSession)
        
        /// <summary>
        /// Get formatted current game time string (HH:MM:SS)
        /// </summary>
        public string GetFormattedGameTime()
        {
            return TimeUtilities.FormatTimeFromSeconds(_gameTime);
        }
        
        /// <summary>
        /// Get formatted current session time string (HH:MM:SS)
        /// </summary>
        public string GetFormattedSessionTime()
        {
            return TimeUtilities.FormatTimeFromSeconds(_sessionTime);
        }
        

        
        /// <summary>
        /// Get formatted saved playtime - delegates to utility
        /// </summary>
        public string GetSavedFormattedPlayTime(GameSession session)
        {
            return TimeUtilities.GetSavedFormattedPlayTime(session);
        }
        
        /// <summary>
        /// Helper method delegates to utility
        /// </summary>
        public string FormatTimeFromSeconds(float seconds)
        {
            return TimeUtilities.FormatTimeFromSeconds(seconds);
        }
        
        #endregion
        
        #region GameSession Integration (Moved from GameSession)
        
        /// <summary>
        /// Updates a GameSession with current time data from TimeService (moved from GameSession)
        /// Call this before serializing the session to ensure current time data is captured
        /// </summary>
        public void UpdateSessionTimeData(GameSession session)
        {
            if (session == null)
            {
                Debug.LogWarning("[TimeService] Cannot update time data - session is null");
                return;
            }
            
            if (IsInitialized)
            {
                session.SetSavedTimeData(_gameTime, _sessionTime);
                Debug.Log($"[TimeService] Updated session time data - Game: {GetFormattedGameTime()}, Session: {GetFormattedSessionTime()}");
            }
            else
            {
                Debug.LogWarning("[TimeService] TimeService not initialized - using fallback calculation");
                
                // Fallback: Calculate playtime based on session duration if we have no previous data
                if (!session.HasSavedTimeData || session.SavedGameTime == 0f)
                {
                    var sessionDuration = (DateTime.Now - session.SessionStartTime).TotalSeconds;
                    session.SetSavedTimeData((float)sessionDuration, (float)sessionDuration);
                }
            }
        }
        
        #endregion
        
        #region Public Time Methods
        
        /// <summary>
        /// Reset all timers
        /// </summary>
        public void ResetAllTimers()
        {
            _gameTime = 0f;
            _sessionTime = 0f;
            _levelTime = 0f;
        }
        
        /// <summary>
        /// Reset only the level timer (for new scenes)
        /// </summary>
        public void ResetLevelTimer()
        {
            _levelTime = 0f;
        }
        
        /// <summary>
        /// Set saved time data (called when loading a game session)
        /// </summary>
        public void SetSavedTimeData(float gameTime, float sessionTime)
        {
            _gameTime = gameTime;
            _sessionTime = sessionTime;
            _levelTime = 0f; // Reset level time when loading
            
            Debug.Log($"[TimeService] Loaded time data - Game: {FormatTimeFromSeconds(_gameTime)}, Session: {FormatTimeFromSeconds(_sessionTime)}");
        }
        
        /// <summary>
        /// Get time statistics for debugging/display
        /// </summary>
        public TimeStatistics GetTimeStatistics()
        {
            return new TimeStatistics
            {
                GameTime = _gameTime,
                SessionTime = _sessionTime,
                LevelTime = _levelTime,
                IsTrackingGameTime = IsTrackingGameTime,
                IsTrackingSessionTime = IsTrackingSessionTime,
                IsPaused = _isPaused,
                IsInPlayingState = _isInPlayingState
            };
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnGameStateChanged(GameStateChangeEvent evt)
        {
            var wasInPlayingState = _isInPlayingState;
            _isInPlayingState = evt.NewState == GameStateType.Playing;
            
            // If we just entered PlayingState, reset level timer
            if (!wasInPlayingState && _isInPlayingState)
            {
                ResetLevelTimer();
            }
        }
        
        private void OnGamePaused(GamePausedEvent evt)
        {
            _isPaused = true;
        }
        
        private void OnGameResumed(GameResumedEvent evt)
        {
            _isPaused = false;
            
            // Update last update time to prevent time jump when resuming
            _lastUpdateTime = Time.realtimeSinceStartup;
        }
        
        private void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            ResetAllTimers();
        }
        
        private void OnSessionLoaded(SessionLoadedEvent evt)
        {
            Debug.Log("[TimeService] Session loaded event received, loading time data...");
            LoadTimeDataFromSession();
        }
        
        private void OnSceneLoaded(SceneLoadedEvent evt)
        {
            ResetLevelTimer();
        }
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Load time data from the current game session
        /// </summary>
        private void LoadTimeDataFromSession()
        {
            if (_gameDataService?.HasActiveSession() != true) return;

            var session = _gameDataService.CurrentSession;
            if (session.HasSavedTimeData)
            {
                SetSavedTimeData(session.SavedGameTime, session.SavedSessionTime);
            }
            else
            {
                Debug.LogWarning("[TimeService] Session has no saved time data");
            }
        }
        
        #endregion
    }
}
