using System;
using UnityEngine;
using GameFramework.SaveSystem.Data;
using UnityEngine.Serialization;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Minimal wrapper containing essential save file information for UI display
    /// Extracts key data from GameSessionData and PlayerData for save file lists
    /// </summary>
    [System.Serializable]
    public class SaveFileInfo
    {
        #region Serialized Fields
        [SerializeField] private string fileName;
        [SerializeField] private string playerName;
        [SerializeField] private string currentScene;
        [SerializeField] private long lastSaveTimeTicks;
        [SerializeField] private float gameTime; 
        [SerializeField] private bool wasAutoSaved;
        #endregion

        #region Public Properties
        public string FileName => fileName;
        public string PlayerName => playerName;
        public string CurrentScene => currentScene;
        public float GameTime => gameTime;
        public bool WasAutoSaved => wasAutoSaved;
        
        /// <summary>
        /// Last save time as DateTime (converts from ticks)
        /// </summary>
        public DateTime LastSaveTime 
        { 
            get => new DateTime(lastSaveTimeTicks);
            private set => lastSaveTimeTicks = value.Ticks;
        }

        /// <summary>
        /// Ticks representation for serialization
        /// </summary>
        public long LastSaveTimeTicks => lastSaveTimeTicks;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public SaveFileInfo()
        {
            fileName = string.Empty;
            playerName = "Unknown";
            currentScene = "Unknown";
            lastSaveTimeTicks = DateTime.Now.Ticks;
            gameTime = 0f;
            wasAutoSaved = false;
        }

        /// <summary>
        /// Constructor from SaveFileData
        /// </summary>
        public SaveFileInfo(string fileName, SaveFileData saveData)
        {
            this.fileName = fileName ?? string.Empty;
            LastSaveTime = saveData.SaveTime; // Uses property setter to convert to ticks
            wasAutoSaved = saveData.WasAutoSave;

            // Extract GameSessionData
            if (saveData.SavedObjects.TryGetValue("GameSessionData", out var gameSessionObject))
            {
                try
                {
                    var gameSessionData = gameSessionObject.GetData<GameSessionData>();
                    currentScene = gameSessionData?.CurrentScene ?? "Unknown";
                    gameTime = gameSessionData?.GameTime ?? 0f;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveFileInfo] Failed to extract GameSessionData from {fileName}: {ex.Message}");
                    currentScene = "Unknown";
                    gameTime = 0f;
                }
            }
            else
            {
                currentScene = "Unknown";
                gameTime = 0f;
            }

            // Extract PlayerData
            if (saveData.SavedObjects.TryGetValue("PlayerData", out var playerObject))
            {
                try
                {
                    var playerData = playerObject.GetData<PlayerData>();
                    playerName = playerData?.PlayerName ?? "Unknown";
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveFileInfo] Failed to extract PlayerData from {fileName}: {ex.Message}");
                    playerName = "Unknown";
                }
            }
            else
            {
                playerName = "Unknown";
            }
        }
        #endregion

        #region Display Helper Methods
        /// <summary>
        /// Gets formatted save time string for UI display
        /// </summary>
        /// <param name="format">DateTime format string</param>
        /// <returns>Formatted date/time string</returns>
        public string GetFormattedSaveTime(string format = "yyyy/MM/dd HH:mm:ss")
        {
            return LastSaveTime.ToString(format);
        }

        /// <summary>
        /// Gets formatted game time string (hours:minutes:seconds)
        /// </summary>
        /// <returns>Formatted game time string</returns>
        public string GetFormattedGameTime()
        {
            var timeSpan = TimeSpan.FromSeconds(gameTime);
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        /// <summary>
        /// Gets save type display string
        /// </summary>
        /// <returns>User-friendly save type string</returns>
        public string GetSaveTypeString()
        {
            return wasAutoSaved ? "Auto Save" : "Manual Save";
        }

        /// <summary>
        /// Gets a short display name for the save file (without extension)
        /// </summary>
        /// <returns>Display name without file extension</returns>
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(fileName)) return "Unknown";
            return System.IO.Path.GetFileNameWithoutExtension(fileName);
        }
        #endregion

        #region Overrides
        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"SaveFileInfo: {fileName} | Player: {playerName} | Scene: {currentScene} | " +
                   $"Time: {GetFormattedGameTime()} | Saved: {GetFormattedSaveTime()} | " +
                   $"Type: {GetSaveTypeString()}";
        }

        /// <summary>
        /// Equality comparison based on file name
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is SaveFileInfo other)
            {
                return string.Equals(fileName, other.fileName, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Hash code based on file name
        /// </summary>
        public override int GetHashCode()
        {
            return fileName?.GetHashCode() ?? 0;
        }
        #endregion
    }
}
