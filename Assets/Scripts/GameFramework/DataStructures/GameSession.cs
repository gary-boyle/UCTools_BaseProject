using System;
using System.Collections.Generic;
using GameFramework.StateMachine.Data;
using GameFramework.Services.Interfaces;
using UnityEngine;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Central game session data - single source of truth for all game state
    /// Replaces the fragmented data structures with a unified, extensible system
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
        public float totalPlayTimeSeconds = 0f;
        
        [Header("Player State")]
        public PlayerState player = new PlayerState();
        
        [Header("Game Progress")]
        public GameProgress progress = new GameProgress();
        
        [Header("Custom Data")]
        public Dictionary<string, object> customData = new Dictionary<string, object>();
        
        // Pause-aware time tracking (non-serialized, runtime only)
        [System.NonSerialized]
        private IPauseService _pauseService;
        [System.NonSerialized]
        private DateTime? _pauseStartTime;
        [System.NonSerialized]
        private float _totalPausedTimeSeconds = 0f;
        [System.NonSerialized]
        private bool _isTrackingTime = false;
        
        /// <summary>
        /// Initialize the session with pause service for accurate time tracking
        /// Call this after creating/loading a session
        /// </summary>
        public void Initialize(IPauseService pauseService)
        {
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
            
            // Subscribe to pause events if not already tracking
            if (!_isTrackingTime)
            {
                _pauseService.OnPauseStateChanged += OnPauseStateChanged;
                _isTrackingTime = true;
                
                // If game is currently paused, start tracking pause time
                if (_pauseService.IsPaused)
                {
                    _pauseStartTime = DateTime.Now;
                }
                
                Debug.Log("[GameSession] Time tracking initialized with pause awareness");
            }
        }
        
        /// <summary>
        /// Call this when the session is being destroyed or replaced
        /// </summary>
        public void Cleanup()
        {
            if (_pauseService != null && _isTrackingTime)
            {
                _pauseService.OnPauseStateChanged -= OnPauseStateChanged;
                _isTrackingTime = false;
            }
        }
        
        /// <summary>
        /// Creates a new game session with specified parameters
        /// </summary>
        public static GameSession CreateNewGame(string playerName, string difficulty, string startingScene)
        {
            return new GameSession
            {
                playerName = playerName,
                difficulty = difficulty,
                currentScene = startingScene,
                sessionStartTime = DateTime.Now,
                lastSaveTime = DateTime.Now,
                totalPlayTimeSeconds = 0f,
                player = PlayerState.CreateDefault(difficulty),
                progress = GameProgress.CreateDefault(),
                customData = new Dictionary<string, object>
                {
                    ["creationTime"] = DateTime.Now.ToString(),
                    ["isNewGame"] = true,
                    ["startingPosition"] = "DefaultSpawn"
                }
            };
        }
        
        /// <summary>
        /// Updates play time based on current session, respecting pause state
        /// </summary>
        public void UpdatePlayTime()
        {
            if (_pauseService != null && _pauseService.IsPaused)
            {
                // Don't update playtime while paused
                return;
            }
            
            // Calculate total elapsed time minus paused time
            var totalElapsed = (float)(DateTime.Now - sessionStartTime).TotalSeconds;
            var currentPausedTime = GetCurrentTotalPausedTime();
            totalPlayTimeSeconds = Mathf.Max(0f, totalElapsed - currentPausedTime);
        }
        
        /// <summary>
        /// Adjusts the session start time based on already accumulated playtime
        /// Call this after loading a save to ensure correct playtime calculation
        /// </summary>
        public void AdjustSessionStartTimeForLoad()
        {
            // Reset pause tracking for new session
            _totalPausedTimeSeconds = 0f;
            _pauseStartTime = null;
            
            // Set session start time to: current time - already accumulated playtime
            sessionStartTime = DateTime.Now.AddSeconds(-totalPlayTimeSeconds);
            Debug.Log($"[GameSession] Adjusted session start time for loaded save. Total playtime: {totalPlayTimeSeconds:F1}s");
        }
        
        /// <summary>
        /// Gets the current playtime without modifying the session
        /// Useful for display purposes
        /// </summary>
        public float GetCurrentPlayTime()
        {
            if (_pauseService != null && _pauseService.IsPaused)
            {
                // Return the playtime as it was when pause started
                return totalPlayTimeSeconds;
            }
            
            var totalElapsed = (float)(DateTime.Now - sessionStartTime).TotalSeconds;
            var currentPausedTime = GetCurrentTotalPausedTime();
            Debug.Log($"!!!!{totalElapsed}!!!!! {currentPausedTime}!!!!!");
            return Mathf.Max(0f, totalElapsed - currentPausedTime);
        }
        
        /// <summary>
        /// Gets the total time spent paused during this session
        /// </summary>
        private float GetCurrentTotalPausedTime()
        {
            float currentPausedTime = _totalPausedTimeSeconds;
            
            // If currently paused, add the time since pause started
            if (_pauseStartTime.HasValue)
            {
                currentPausedTime += (float)(DateTime.Now - _pauseStartTime.Value).TotalSeconds;
            }
            
            return currentPausedTime;
        }
        
        /// <summary>
        /// Handle pause state changes to track paused time accurately
        /// </summary>
        private void OnPauseStateChanged(bool isPaused)
        {
            if (isPaused)
            {
                // Game was paused - start tracking pause time
                _pauseStartTime = DateTime.Now;
                Debug.Log("[GameSession] Pause started - stopping playtime accumulation");
            }
            else
            {
                // Game was resumed - add to total paused time
                if (_pauseStartTime.HasValue)
                {
                    var pauseDuration = (float)(DateTime.Now - _pauseStartTime.Value).TotalSeconds;
                    _totalPausedTimeSeconds += pauseDuration;
                    _pauseStartTime = null;
                    
                    Debug.Log($"[GameSession] Pause ended - added {pauseDuration:F1}s to total paused time ({_totalPausedTimeSeconds:F1}s total)");
                }
            }
        }
        
        /// <summary>
        /// Converts session to loading configuration for state transitions
        /// </summary>
        public LoadingConfiguration ToLoadingConfiguration()
        {
            // Update playtime before creating config
            UpdatePlayTime();
            
            var config = LoadingConfiguration.LoadSave(currentScene, customData);
            config.PlayerName = playerName;
            config.GameData["difficulty"] = difficulty;
            config.GameData["playerLevel"] = player.level;
            config.GameData["totalPlayTime"] = totalPlayTimeSeconds;
            return config;
        }
    }
}
