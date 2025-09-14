using System;
using UnityEngine;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Pure data container for game session information
    /// Contains only serializable data fields and basic accessors
    /// No business logic - just data storage and retrieval
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
        
        [Header("Time Tracking - Serialized Fields")]
        [SerializeField] private float _savedGameTime = 0f;      // Serialized playtime data
        [SerializeField] private float _savedSessionTime = 0f;   // Serialized session time data
        [SerializeField] private bool _hasTimeData = false;      // Flag to know if we have saved time data
        
        [SerializeField] public bool WasAutoSave = false;      // Flag to know if the game was an autosave
        
        #region DateTime Properties
        
        /// <summary>
        /// Gets/Sets the session start time with proper serialization support
        /// </summary>
        public DateTime SessionStartTime
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
        public DateTime LastSaveTime
        {
            get 
            {
                if (_lastSaveTimeTicks == 0)
                    return DateTime.Now; // Fallback for uninitialized data
                return new DateTime(_lastSaveTimeTicks);
            }
            set { _lastSaveTimeTicks = value.Ticks; }
        }
        
        #endregion
        
        #region Time Data Access
        
        /// <summary>
        /// Gets the saved game time in seconds
        /// </summary>
        public float SavedGameTime => _savedGameTime;
        
        /// <summary>
        /// Gets the saved session time in seconds  
        /// </summary>
        public float SavedSessionTime => _savedSessionTime;
        
        /// <summary>
        /// Sets the saved time data (used by TimeService)
        /// </summary>
        public void SetSavedTimeData(float gameTime, float sessionTime)
        {
            _savedGameTime = gameTime;
            _savedSessionTime = sessionTime;
            _hasTimeData = true;
        }
        
        /// <summary>
        /// Checks if this session has saved time data
        /// </summary>
        public bool HasSavedTimeData => _hasTimeData;
        
        /// <summary>
        /// Marks that time data has been initialized
        /// </summary>
        public void SetHasTimeData(bool hasData)
        {
            _hasTimeData = hasData;
        }
        
        #endregion
        
        #region Scene Management
        
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
        
        #endregion
        
        #region Debug
        
        /// <summary>
        /// Gets a summary string for debugging
        /// </summary>
        public override string ToString()
        {
            return $"GameSession[Player: {playerName}, Difficulty: {difficulty}, Scene: {currentScene}, " +
                   $"SavedGameTime: {_savedGameTime:F1}s, HasTimeData: {_hasTimeData}, " +
                   $"LastSave: {LastSaveTime}]";
        }
        
        #endregion
    }
}
