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
        public DateTime sessionStartTime;
        public DateTime lastSaveTime;
        
        [Header("Time Tracking - Serialized Fields")]
        [SerializeField] private float _savedGameTime = 0f;      // Serialized playtime data
        [SerializeField] private float _savedSessionTime = 0f;   // Serialized session time data
        [SerializeField] private bool _hasTimeData = false;      // Flag to know if we have saved time data
        
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
        /// Gets the current session time - uses TimeService if available, fallback to saved data
        /// </summary>
        public float SessionTimeSeconds
        {
            get
            {
                var timeService = GameManager.GetService<ITimeService>();
                if (timeService != null && timeService.IsInitialized)
                {
                    return timeService.SessionTime;
                }
                return _savedSessionTime; // Fallback to saved data when TimeService not available
            }
        }
        
        /// <summary>
        /// Gets formatted playtime string for display (HH:MM:SS)
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
        /// Gets formatted session time string for display (HH:MM:SS)
        /// </summary>
        public string FormattedSessionTime
        {
            get
            {
                var timeService = GameManager.GetService<ITimeService>();
                if (timeService != null && timeService.IsInitialized)
                {
                    return timeService.GetFormattedSessionTime();
                }
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
            if (timeService != null && timeService.IsInitialized)
            {
                _savedGameTime = timeService.GameTime;
                _savedSessionTime = timeService.SessionTime;
                _hasTimeData = true;
                
                Debug.Log($"[GameSession] Updated time data from TimeService - Game: {FormatTimeFromSeconds(_savedGameTime)}, Session: {FormatTimeFromSeconds(_savedSessionTime)}");
            }
            else
            {
                Debug.LogWarning("[GameSession] TimeService not available - time data not updated");
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
        /// Gets the serialized/saved playtime data (for UI display when TimeService not available)
        /// </summary>
        public float SavedGameTime => _savedGameTime;
        
        /// <summary>
        /// Gets the serialized/saved session time data
        /// </summary>
        public float SavedSessionTime => _savedSessionTime;
        
        /// <summary>
        /// Checks if this session has saved time data
        /// </summary>
        public bool HasSavedTimeData => _hasTimeData;
        
        /// <summary>
        /// Creates a new game session with specified parameters
        /// </summary>
        public static GameSession CreateNewGame(string playerName, string difficulty, string startingScene)
        {
            var session = new GameSession
            {
                playerName = playerName,
                difficulty = difficulty,
                currentScene = startingScene,
                sessionStartTime = DateTime.Now,
                lastSaveTime = DateTime.Now,
                player = PlayerState.CreateDefault(difficulty),
                progress = GameProgress.CreateDefault(),
                _savedGameTime = 0f,      // Initialize time data
                _savedSessionTime = 0f,
                _hasTimeData = true,      // Mark as having time data
                customData = new Dictionary<string, object>
                {
                    ["creationTime"] = DateTime.Now.ToString(),
                    ["isNewGame"] = true,
                    ["startingPosition"] = "DefaultSpawn"
                }
            };
            
            Debug.Log($"[GameSession] Created new game session for player '{playerName}' on difficulty '{difficulty}'");
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
            Debug.Log($"[GameSession] Updated last save time: {lastSaveTime} with playtime data");
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
        /// Gets the age of this session (time since creation)
        /// </summary>
        public TimeSpan GetSessionAge()
        {
            return DateTime.Now - sessionStartTime;
        }
        
        /// <summary>
        /// Gets the time since last save
        /// </summary>
        public TimeSpan GetTimeSinceLastSave()
        {
            return DateTime.Now - lastSaveTime;
        }
        
        /// <summary>
        /// Converts session to loading configuration for state transitions
        /// </summary>
        public LoadingConfiguration ToLoadingConfiguration()
        {
            var config = LoadingConfiguration.LoadSave(currentScene, customData);
            config.PlayerName = playerName;
            config.GameData["difficulty"] = difficulty;
            config.GameData["playerLevel"] = player.level;
            config.GameData["totalPlayTime"] = TotalPlayTimeSeconds;
            config.GameData["sessionTime"] = SessionTimeSeconds;
            config.GameData["sessionAge"] = GetSessionAge().TotalSeconds;
            
            Debug.Log($"[GameSession] Created loading configuration - Playtime: {FormattedPlayTime}, Scene: {currentScene}");
            return config;
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
            
            var previousScene = currentScene;
            currentScene = sceneName;
            
            Debug.Log($"[GameSession] Scene changed: {previousScene} -> {currentScene}");
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
            Debug.Log($"[GameSession] Set custom data '{key}': {value}");
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
                   $"Playtime: {FormattedPlayTime}, Level: {player.level}, HasTimeData: {_hasTimeData}]";
        }
    }
}
