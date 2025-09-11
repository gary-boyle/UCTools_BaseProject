using System;
using System.Collections.Generic;
using GameFramework.StateMachine.Data;
using GameFramework.Services.Interfaces;
using GameFramework.Core;
using UnityEngine;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Central game session data - single source of truth for all game state
    /// Stores playtime data in serializable fields while providing TimeService integration
    /// Ensures playtime persists correctly across save/load operations
    /// </summary>
    [Serializable]
    public class GameSession
    {
        [Header("Session Info")]
        public string playerName = "Player";
        public string difficulty = "Normal";
        public string currentScene = "";
        
        [Header("DateTime Fields - Serializable")]
        [SerializeField] private long _sessionStartTimeTicks;
        [SerializeField] private long _lastSaveTimeTicks;
        
        /// <summary>
        /// Gets/Sets the session start time with proper serialization support
        /// </summary>
        public DateTime sessionStartTime
        {
            get 
            {
                if (_sessionStartTimeTicks == 0)
                    return DateTime.Now; // Fallback for uninitialized data
                return new DateTime(_sessionStartTimeTicks);
            }
            set { _sessionStartTimeTicks = value.Ticks; }
        }
        
        /// <summary>
        /// Gets/Sets the last save time with proper serialization support
        /// </summary>
        public DateTime lastSaveTime
        {
            get 
            {
                if (_lastSaveTimeTicks == 0)
                    return DateTime.Now; // Fallback for uninitialized data
                return new DateTime(_lastSaveTimeTicks);
            }
            set { _lastSaveTimeTicks = value.Ticks; }
        }
        
        [Header("Time Tracking - Serialized Fields")]
        [SerializeField] private float _savedGameTime = 0f;      // Serialized playtime data
        [SerializeField] private float _savedSessionTime = 0f;   // Serialized session time data
        [SerializeField] private bool _hasTimeData = false;      // Flag to know if we have saved time data
        
        [SerializeField] public bool WasAutoSave = false;      // Flag to know if the game was an autosave

        [Header("Player State")]
        public PlayerState player = new PlayerState();
        
        [Header("Game Progress")]
        public GameProgress progress = new GameProgress();
        
        [Header("Custom Data")]
        public Dictionary<string, object> customData = new Dictionary<string, object>();
        
        /// <summary>
        /// Gets the current total playtime - uses TimeService if available, fallback to saved data
        /// </summary>
        public float TotalPlayTimeSeconds
        {
            get
            {
                var timeService = GameManager.GetService<ITimeService>();
                if (timeService != null && timeService.IsInitialized)
                {
                    return timeService.GameTime;
                }
                return _savedGameTime; // Fallback to saved data when TimeService not available
            }
        }
        
        /// <summary>
        /// Gets formatted playtime string for display (HH:MM:SS)
        /// Uses current TimeService if available, fallback to saved data
        /// </summary>
        public string FormattedPlayTime
        {
            get
            {
                var timeService = GameManager.GetService<ITimeService>();
                if (timeService != null && timeService.IsInitialized)
                {
                    return timeService.GetFormattedGameTime();
                }
                return FormatTimeFromSeconds(_savedGameTime);
            }
        }
        
        /// <summary>
        /// Gets the saved playtime string for display in save file lists (HH:MM:SS)
        /// Always uses the saved time data, not current TimeService data
        /// </summary>
        public string SavedFormattedPlayTime
        {
            get
            {
                return FormatTimeFromSeconds(_savedGameTime);
            }
        }
        
        /// <summary>
        /// Gets the saved session time string for display in save file lists (HH:MM:SS)
        /// Always uses the saved time data, not current TimeService data
        /// </summary>
        public string SavedFormattedSessionTime
        {
            get
            {
                return FormatTimeFromSeconds(_savedSessionTime);
            }
        }

        /// <summary>
        /// Updates the saved time data from TimeService before saving
        /// Call this before serializing the session to ensure current time data is captured
        /// </summary>
        public void UpdateTimeDataFromService()
        {
            var timeService = GameManager.GetService<ITimeService>();
    
            // Debug logs
            Debug.Log($"[GameSession] UpdateTimeDataFromService - TimeService: {timeService != null}");
    
            if (timeService != null)
            {
                Debug.Log($"[GameSession] TimeService IsInitialized: {timeService.IsInitialized}");
                Debug.Log($"[GameSession] TimeService GameTime: {timeService.GameTime}");
                Debug.Log($"[GameSession] TimeService SessionTime: {timeService.SessionTime}");
            }
    
            if (timeService != null && timeService.IsInitialized)
            {
                _savedGameTime = timeService.GameTime;
                _savedSessionTime = timeService.SessionTime;
                _hasTimeData = true;
        
                Debug.Log($"[GameSession] Updated time data from TimeService - Game: {FormatTimeFromSeconds(_savedGameTime)}, Session: {FormatTimeFromSeconds(_savedSessionTime)}");
            }
            else
            {
                Debug.LogWarning("[GameSession] TimeService not available - using fallback calculation");
                
                // Fallback: Calculate playtime based on session duration if we have no previous data
                if (!_hasTimeData || _savedGameTime == 0f)
                {
                    var sessionDuration = (DateTime.Now - sessionStartTime).TotalSeconds;
                    _savedGameTime = (float)sessionDuration;
                    _savedSessionTime = (float)sessionDuration;
                }
                
                _hasTimeData = true;
                Debug.Log($"[GameSession] Using fallback time data: {FormatTimeFromSeconds(_savedGameTime)}");
            }
        }
        
        /// <summary>
        /// Restores time data to TimeService after loading (if supported by TimeService)
        /// </summary>
        public void RestoreTimeDataToService()
        {
            if (!_hasTimeData) return;
            
            var gameDataService = GameManager.GetService<IGameDataService>();
            if (gameDataService != null)
            {
                // Store time data in GameDataService for TimeService to pick up
                gameDataService.SetCustomData("GameTime", _savedGameTime);
                gameDataService.SetCustomData("SessionTime", _savedSessionTime);
                
                Debug.Log($"[GameSession] Restored time data to GameDataService - Game: {FormatTimeFromSeconds(_savedGameTime)}, Session: {FormatTimeFromSeconds(_savedSessionTime)}");
            }
        }
          
        /// <summary>
        /// Creates a new game session with specified parameters
        /// </summary>
        public static GameSession CreateNewGame(string playerName, string difficulty, string startingScene)
        {
            var now = DateTime.Now;
            var session = new GameSession
            {
                playerName = playerName,
                difficulty = difficulty,
                currentScene = startingScene,
                sessionStartTime = now,      // Uses the property, will set _sessionStartTimeTicks
                lastSaveTime = now,          // Uses the property, will set _lastSaveTimeTicks
                player = PlayerState.CreateDefault(difficulty),
                progress = GameProgress.CreateDefault(),
                _savedGameTime = 0f,         // Initialize time data
                _savedSessionTime = 0f,
                _hasTimeData = true,         // Mark as having time data
                WasAutoSave = false,
                customData = new Dictionary<string, object>
                {
                    ["creationTime"] = now.ToString("O"), // ISO 8601 format for consistency
                    ["isNewGame"] = true,
                    ["startingPosition"] = "DefaultSpawn"
                }
            };
            
            return session;
        }
        
        /// <summary>
        /// Updates the last save time and captures current playtime data
        /// Called when the game is saved
        /// </summary>
        public void UpdateLastSaveTime()
        {
            lastSaveTime = DateTime.Now;
            
            UpdateTimeDataFromService(); // Capture current time data
        }
        
        /// <summary>
        /// Gets playtime statistics for debugging or display
        /// </summary>
        public PlayTimeInfo GetPlayTimeInfo()
        {
            var timeService = GameManager.GetService<ITimeService>();
            if (timeService != null && timeService.IsInitialized)
            {
                return new PlayTimeInfo
                {
                    GameTime = timeService.GameTime,
                    SessionTime = timeService.SessionTime,
                    FormattedGameTime = timeService.GetFormattedGameTime(),
                    FormattedSessionTime = timeService.GetFormattedSessionTime(),
                    IsTracking = timeService.IsTrackingGameTime
                };
            }
            
            // Fallback to saved data
            return new PlayTimeInfo
            {
                GameTime = _savedGameTime,
                SessionTime = _savedSessionTime,
                FormattedGameTime = FormatTimeFromSeconds(_savedGameTime),
                FormattedSessionTime = FormatTimeFromSeconds(_savedSessionTime),
                IsTracking = false // Can't track when TimeService not available
            };
        }
        
        /// <summary>
        /// Helper method to format time from seconds
        /// </summary>
        private string FormatTimeFromSeconds(float seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}:{2:D2}", 
                timeSpan.Hours, 
                timeSpan.Minutes, 
                timeSpan.Seconds);
        }
        
        
        /// <summary>
        /// Updates the current scene reference
        /// </summary>
        public void SetCurrentScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[GameSession] Attempted to set empty scene name");
                return;
            }
            
            currentScene = sceneName;
        }
        
        /// <summary>
        /// Adds or updates custom data
        /// </summary>
        public void SetCustomData<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[GameSession] Attempted to set custom data with empty key");
                return;
            }
            
            customData[key] = value;
        }
        
        /// <summary>
        /// Gets custom data with optional default value
        /// </summary>
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(key) || !customData.ContainsKey(key))
            {
                return defaultValue;
            }
            
            try
            {
                return (T)customData[key];
            }
            catch (InvalidCastException)
            {
                Debug.LogWarning($"[GameSession] Failed to cast custom data '{key}' to type {typeof(T).Name}");
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Gets a summary string for debugging
        /// </summary>
        public override string ToString()
        {
            return $"GameSession[Player: {playerName}, Difficulty: {difficulty}, Scene: {currentScene}, " +
                   $"Playtime: {FormattedPlayTime}, Level: {player.Level}, HasTimeData: {_hasTimeData}, " +
                   $"LastSave: {lastSaveTime}]";
        }
    }
}
