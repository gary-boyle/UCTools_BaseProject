using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.GameData.Events;
using GameFramework.Services.Data;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.Utilities;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// TimeService manages game time tracking by directly updating GameSessionData
    /// GameSessionData is the single source of truth for game time
    /// Uses double precision internally for accurate tracking of large time values
    /// </summary>
    public class TimeService : ITimeService, IUpdatable
    {
        #region Properties and Fields
        
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Get current game time with double precision from GameSessionData
        /// </summary>
        public long GameTime => _gameDataService?.GetGameSessionData()?.GameTime ?? 0;
        
        // Level time stays local since it's not saved
        private long _levelTime = 0;
        private double _deltaTimeAccumulator = 0.0;
        
        // State tracking
        public bool IsTrackingGameTime => _isInPlayingState && !_isPaused;
        
        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly IGameDataService _gameDataService;
        
        // State flags
        private bool _isInPlayingState = false;
        private bool _isPaused = false;
        private bool _isInitialized = false;
        
        // For delta time calculations - use double precision
        private double _lastUpdateTime;
        
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
            _eventSystem.Subscribe<SceneLoadedEvent>(OnSceneLoaded);

            // Note: No need to subscribe to GameDataLoadedEvent or SaveRequestedEvent
            // since GameSessionData is the single source of truth

            // Initialize time tracking with double precision
            _lastUpdateTime = Time.realtimeSinceStartupAsDouble;
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
            _eventSystem?.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);

            _isInitialized = false;
            IsInitialized = false;
        }
        
        #endregion
        
        #region Update Loop
        
        /// <summary>
        /// Update time tracking - directly updates GameSessionData.GameTime
        /// GameSessionData is the single source of truth for game time
        /// Uses double precision for accurate tracking of large time values
        /// </summary>

        public void Update()
        {
            if (!_isInitialized) return;
            
            var gameSession = _gameDataService?.GetGameSessionData();
            if (gameSession == null) return;
            
            // Calculate delta time using double precision real time (unaffected by Time.timeScale)
            double currentTime = Time.realtimeSinceStartupAsDouble;
            double deltaTime = currentTime - _lastUpdateTime;
            _lastUpdateTime = currentTime;
            
            // Update timers based on current state
            if (!_isPaused && _isInPlayingState)
            {
                // Accumulate deltaTime
                _deltaTimeAccumulator += deltaTime;
            
                // Increment GameTime when accumulated deltaTime exceeds 1
                while (_deltaTimeAccumulator >= 1.0)
                {
                    gameSession.GameTime++;
                    _levelTime++;
            
                    _deltaTimeAccumulator = 0.0;
                }
            
                // Update level time
            }
        }

        #endregion
        
        #region Time Formatting Utilities
        
        /// <summary>
        /// Get formatted current game time string with double precision
        /// </summary>
        public string GetFormattedGameTime()
        {
            return TimeUtilities.FormatTimeFromSeconds(GameTime);
        }

        #endregion
        
        #region Public Time Methods
        
        /// <summary>
        /// Reset all timers
        /// </summary>
        public void ResetAllTimers()
        {
            var gameSession = _gameDataService?.GetGameSessionData();
            if (gameSession != null)
            {
                gameSession.GameTime = 0;
            }
            _levelTime = 0;
        }
        
        /// <summary>
        /// Reset only the level timer (for new scenes)
        /// </summary>
        public void ResetLevelTimer()
        {
            _levelTime = 0;
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
            _lastUpdateTime = Time.realtimeSinceStartupAsDouble;
        }
        
        private void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            ResetAllTimers();
        }
        
        private void OnSceneLoaded(SceneLoadedEvent evt)
        {
            ResetLevelTimer();
        }
        
        #endregion
    }
}
