using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// TimeService manages game time tracking with proper pause handling and state awareness.
    /// Tracks time only when in PlayingState and not paused.
    /// Provides formatted time display and session tracking capabilities.
    /// 
    /// Design: Uses event-driven architecture to respond to game state changes and pause events.
    /// Pros: Accurate time tracking, integrates with pause system, provides multiple time formats
    /// Cons: Depends on proper event firing from other systems
    /// </summary>
    public class TimeService : ITimeService, IUpdatable
    {
        #region Properties and Fields
        
        public bool IsInitialized { get; private set; }
        
        // Time tracking properties
        public float GameTime => _gameTime;
        public float SessionTime => _sessionTime;
        public float LevelTime => _levelTime;
        public TimeSpan GameTimeSpan => TimeSpan.FromSeconds(_gameTime);
        public TimeSpan SessionTimeSpan => TimeSpan.FromSeconds(_sessionTime);
        public TimeSpan LevelTimeSpan => TimeSpan.FromSeconds(_levelTime);
        
        // State tracking
        public bool IsTrackingGameTime => _isInPlayingState && !_isPaused;
        public bool IsTrackingSessionTime => _isInitialized && !_isPaused;
        
        // Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly IPauseService _pauseService;
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
        
        // Events
        public event Action<float> OnGameTimeChanged;
        public event Action<float> OnSessionTimeChanged;
        public event Action<float> OnLevelTimeChanged;
        public event Action OnTimersReset;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public TimeService(
            IEventSystem eventSystem, 
            IPauseService pauseService,
            IGameDataService gameDataService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }
        
        #endregion
        
        #region Initialization and Shutdown
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[TimeService] Initializing time service...");
            
            // Subscribe to game state changes
            _eventSystem.Subscribe<GameStateChangeEvent>(OnGameStateChanged);
            
            // Subscribe to pause events
            _eventSystem.Subscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem.Subscribe<GameResumedEvent>(OnGameResumed);
            
            // Subscribe to game lifecycle events
            _eventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            _eventSystem.Subscribe<LoadGameEvent>(OnGameLoaded);
            _eventSystem.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
            
            // Initialize time tracking
            _lastUpdateTime = Time.realtimeSinceStartup;
            _isInitialized = true;
            
            // Load existing time data if we have an active session
            LoadTimeDataFromSession();
            
            IsInitialized = true;
            await Task.CompletedTask;
            
            Debug.Log("[TimeService] Time service initialized successfully");
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[TimeService] Shutting down time service...");
            
            // Save time data to session
            SaveTimeDataToSession();
            
            // Unsubscribe from events
            _eventSystem?.Unsubscribe<GameStateChangeEvent>(OnGameStateChanged);
            _eventSystem?.Unsubscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem?.Unsubscribe<GameResumedEvent>(OnGameResumed);
            _eventSystem?.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            _eventSystem?.Unsubscribe<LoadGameEvent>(OnGameLoaded);
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

            // Update session time (always tracking when initialized and not paused)
            if (!_isPaused)
            {
                _sessionTime += deltaTime;
                OnSessionTimeChanged?.Invoke(_sessionTime);
            }
            
            // Update game time (only when in PlayingState and not paused)
            if (IsTrackingGameTime)
            {
                _gameTime += deltaTime;
                _levelTime += deltaTime;
                
                OnGameTimeChanged?.Invoke(_gameTime);
                OnLevelTimeChanged?.Invoke(_levelTime);
            }
        }
        
        #endregion
        
        #region Public Time Methods
        
        /// <summary>
        /// Get formatted game time string (HH:MM:SS)
        /// </summary>
        public string GetFormattedGameTime()
        {
            return FormatTime(_gameTime);
        }
        
        /// <summary>
        /// Get formatted session time string (HH:MM:SS)
        /// </summary>
        public string GetFormattedSessionTime()
        {
            return FormatTime(_sessionTime);
        }
        
        /// <summary>
        /// Get formatted level time string (HH:MM:SS)
        /// </summary>
        public string GetFormattedLevelTime()
        {
            return FormatTime(_levelTime);
        }
        
        /// <summary>
        /// Reset all timers
        /// </summary>
        public void ResetAllTimers()
        {
            Debug.Log("[TimeService] Resetting all timers...");
            
            _gameTime = 0f;
            _sessionTime = 0f;
            _levelTime = 0f;
            
            OnTimersReset?.Invoke();
            
            Debug.Log("[TimeService] All timers reset");
        }
        
        /// <summary>
        /// Reset only the level timer (for new scenes)
        /// </summary>
        public void ResetLevelTimer()
        {
            Debug.Log("[TimeService] Resetting level timer...");
            _levelTime = 0f;
            OnLevelTimeChanged?.Invoke(_levelTime);
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
            Debug.Log("[TimeService] Game paused - stopping time tracking");
        }
        
        private void OnGameResumed(GameResumedEvent evt)
        {
            _isPaused = false;
            
            // Update last update time to prevent time jump when resuming
            _lastUpdateTime = Time.realtimeSinceStartup;
            
            Debug.Log("[TimeService] Game resumed - resuming time tracking");
        }
        
        private void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            Debug.Log("[TimeService] New game started - resetting timers");
            ResetAllTimers();
        }
        
        private void OnGameLoaded(LoadGameEvent evt)
        {
            Debug.Log("[TimeService] Game loaded - loading time data from session");
            LoadTimeDataFromSession();
        }
        
        private void OnSceneLoaded(SceneLoadedEvent evt)
        {
            Debug.Log($"[TimeService] Scene loaded: {evt.SceneName} - resetting level timer");
            ResetLevelTimer();
        }
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Format seconds into HH:MM:SS string
        /// </summary>
        private string FormatTime(float seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}:{2:D2}", 
                timeSpan.Hours, 
                timeSpan.Minutes, 
                timeSpan.Seconds);
        }
        
        /// <summary>
        /// Save time data to the current game session
        /// </summary>
        private void SaveTimeDataToSession()
        {
            if (_gameDataService?.HasActiveSession() == true)
            {
                _gameDataService.SetCustomData("GameTime", _gameTime);
                _gameDataService.SetCustomData("SessionTime", _sessionTime);
                Debug.Log("[TimeService] Time data saved to session");
            }
        }
        
        /// <summary>
        /// Load time data from the current game session
        /// </summary>
        private void LoadTimeDataFromSession()
        {
            if (_gameDataService?.HasActiveSession() == true)
            {
                _gameTime = _gameDataService.GetCustomData<float>("GameTime", 0f);
                _sessionTime = _gameDataService.GetCustomData<float>("SessionTime", 0f);
                _levelTime = 0f; // Always reset level time when loading
        
                Debug.Log($"[TimeService] Time data loaded from session - Game: {GetFormattedGameTime()}, Session: {GetFormattedSessionTime()}");
        
                // Fire events to notify other systems of the loaded time
                OnGameTimeChanged?.Invoke(_gameTime);
                OnSessionTimeChanged?.Invoke(_sessionTime);
                OnLevelTimeChanged?.Invoke(_levelTime);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Data structure for time statistics
    /// </summary>
    public struct TimeStatistics
    {
        public float GameTime;
        public float SessionTime;
        public float LevelTime;
        public bool IsTrackingGameTime;
        public bool IsTrackingSessionTime;
        public bool IsPaused;
        public bool IsInPlayingState;
        
        public override string ToString()
        {
            return $"GameTime: {TimeSpan.FromSeconds(GameTime):hh\\:mm\\:ss}, " +
                   $"SessionTime: {TimeSpan.FromSeconds(SessionTime):hh\\:mm\\:ss}, " +
                   $"LevelTime: {TimeSpan.FromSeconds(LevelTime):hh\\:mm\\:ss}, " +
                   $"Tracking: Game={IsTrackingGameTime}, Session={IsTrackingSessionTime}, " +
                   $"Paused: {IsPaused}, PlayingState: {IsInPlayingState}";
        }
    }
}
