using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Data;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.Utilities;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// TimeService manages game time tracking and provides time-related utilities
    /// Tracks only GameTime (time spent actually playing when unpaused)
    /// Handles time formatting, playtime info generation, and game time synchronization
    /// </summary>
    public class TimeService : ITimeService, IUpdatable
    {
        #region Properties and Fields
        
        public bool IsInitialized { get; private set; }
        
        // Time tracking properties
        public float GameTime => _gameTime;
        
        // State tracking
        public bool IsTrackingGameTime => _isInPlayingState && !_isPaused;
        
        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly IGameDataService _gameDataService;
        
        // Internal time tracking
        private float _gameTime = 0f;          // Time spent actually playing (PlayingState + not paused)
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
        /// Only tracks game time when in playing state and unpaused
        /// </summary>
        public void Update()
        {
            if (!_isInitialized) return;
    
            // Calculate delta time using real time (unaffected by Time.timeScale)
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - _lastUpdateTime;
            _lastUpdateTime = currentTime;

            // Update timers based on current state
            if (!_isPaused && _isInPlayingState)
            {
                // Only update game time when in playing state and not paused
                _gameTime += deltaTime;
                _levelTime += deltaTime;
            }
        }

        #endregion
        
        #region Time Formatting Utilities
        
        /// <summary>
        /// Get formatted current game time string (HH:MM:SS)
        /// </summary>
        public string GetFormattedGameTime()
        {
            return TimeUtilities.FormatTimeFromSeconds(_gameTime);
        }

        #endregion
        
        #region GameSession Integration

        #endregion
        
        #region Public Time Methods
        
        /// <summary>
        /// Reset all timers
        /// </summary>
        public void ResetAllTimers()
        {
            _gameTime = 0f;
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
        public void SetSavedTimeData(float gameTime)
        {
        }
        
        /// <summary>
        /// Get time statistics for debugging/display
        /// </summary>
        public TimeStatistics GetTimeStatistics()
        {
            return new TimeStatistics
            {
                GameTime = _gameTime,
                LevelTime = _levelTime,
                IsTrackingGameTime = IsTrackingGameTime,
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

            var session = _gameDataService.GetGameSessionData();
            _gameTime = session.GameTime;
            _levelTime = 0f; // Reset level time when loading
        }
        
        #endregion
    }
}
