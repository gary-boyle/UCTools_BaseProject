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
        [SerializeField] private string _fileName;
        [SerializeField] private string _playerName;
        [SerializeField] private string _currentScene;
        [SerializeField] private long _lastSaveTimeTicks;
        [SerializeField] private float _gameTime; 
        [SerializeField] private bool _wasAutoSaved;
        #endregion

        #region Public Properties
        public string FileName => _fileName;
        public string PlayerName => _playerName;
        public string CurrentScene => _currentScene;
        public float GameTime => _gameTime;
        public bool WasAutoSaved => _wasAutoSaved;
        
        /// <summary>
        /// Last save time as DateTime (converts from ticks)
        /// </summary>
        public DateTime LastSaveTime 
        { 
            get => new DateTime(_lastSaveTimeTicks);
            private set => _lastSaveTimeTicks = value.Ticks;
        }

        /// <summary>
        /// Ticks representation for serialization
        /// </summary>
        public long LastSaveTimeTicks => _lastSaveTimeTicks;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public SaveFileInfo()
        {
            _fileName = string.Empty;
            _playerName = "Unknown";
            _currentScene = "Unknown";
            _lastSaveTimeTicks = DateTime.Now.Ticks;
            _gameTime = 0f;
            _wasAutoSaved = false;
        }

        /// <summary>
        /// Constructor from SaveFileData - updated for direct field access
        /// </summary>
        public SaveFileInfo(string fileName, SaveFileData saveData)
        {
            this._fileName = fileName ?? string.Empty;
            LastSaveTime = saveData.SaveTime; // Uses property setter to convert to ticks
            _wasAutoSaved = saveData.WasAutoSave;

            // Extract GameSessionData directly from field
            if (saveData.GameSessionData != null)
            {
                try
                {
                    _currentScene = saveData.GameSessionData.currentScene ?? "Unknown";
                    _gameTime = saveData.GameSessionData.gameTime;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveFileInfo] Failed to extract GameSessionData from {fileName}: {ex.Message}");
                    _currentScene = "Unknown";
                    _gameTime = 0f;
                }
            }
            else
            {
                Debug.LogWarning($"[SaveFileInfo] GameSessionData is null in save file {fileName}");
                _currentScene = "Unknown";
                _gameTime = 0f;
            }

            // Extract PlayerData directly from field
            if (saveData.PlayerData != null)
            {
                try
                {
                    _playerName = saveData.PlayerData.playerName ?? "Unknown";
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveFileInfo] Failed to extract PlayerData from {fileName}: {ex.Message}");
                    _playerName = "Unknown";
                }
            }
            else
            {
                Debug.LogWarning($"[SaveFileInfo] PlayerData is null in save file {fileName}");
                _playerName = "Unknown";
            }
        }
        #endregion

        #region Static Factory Method
        /// <summary>
        /// Creates a SaveFileInfo from a file path by reading and parsing the save file
        /// </summary>
        /// <param name="filePath">Full path to the save file</param>
        /// <returns>SaveFileInfo or null if file cannot be read</returns>
        public static SaveFileInfo CreateFromFile(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogWarning($"[SaveFileInfo] Save file does not exist: {filePath}");
                return null;
            }

            string fileName = System.IO.Path.GetFileName(filePath);

            try
            {
                // Read and parse the save file
                string json = System.IO.File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<SaveFileData>(json);

                if (saveData == null)
                {
                    Debug.LogWarning($"[SaveFileInfo] Failed to parse save file: {fileName}");
                    return CreateCorruptedSaveInfo(fileName);
                }

                return new SaveFileInfo(fileName, saveData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileInfo] Error reading save file {fileName}: {ex.Message}");
                return CreateCorruptedSaveInfo(fileName);
            }
        }

        /// <summary>
        /// Creates a SaveFileInfo for a corrupted save file
        /// </summary>
        private static SaveFileInfo CreateCorruptedSaveInfo(string fileName)
        {
            return new SaveFileInfo
            {
                _fileName = fileName,
                _playerName = "Corrupted Save",
                _currentScene = "Unknown",
                _lastSaveTimeTicks = DateTime.MinValue.Ticks,
                _gameTime = 0f,
                _wasAutoSaved = false
            };
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
            if (LastSaveTime == DateTime.MinValue) return "Corrupted";
            return LastSaveTime.ToString(format);
        }

        /// <summary>
        /// Gets formatted game time string (hours:minutes:seconds)
        /// </summary>
        /// <returns>Formatted game time string</returns>
        public string GetFormattedGameTime()
        {
            if (LastSaveTime == DateTime.MinValue) return "--:--:--";
            
            var timeSpan = TimeSpan.FromSeconds(_gameTime);
            return $"{(int)timeSpan.TotalHours:D2}h :{timeSpan.Minutes:D2}m :{timeSpan.Seconds:D2}s";
        }

        /// <summary>
        /// Gets save type display string
        /// </summary>
        /// <returns>User-friendly save type string</returns>
        public string GetSaveTypeString()
        {
            if (LastSaveTime == DateTime.MinValue) return "Corrupted";
            return _wasAutoSaved ? "Auto Save" : "Manual Save";
        }

        /// <summary>
        /// Gets a short display name for the save file (without extension)
        /// </summary>
        /// <returns>Display name without file extension</returns>
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(_fileName)) return "Unknown";
            return System.IO.Path.GetFileNameWithoutExtension(_fileName);
        }

        /// <summary>
        /// Checks if this save file info represents a valid (non-corrupted) save
        /// </summary>
        public bool IsValid()
        {
            return LastSaveTime != DateTime.MinValue && 
                   !string.IsNullOrEmpty(_fileName) && 
                   _playerName != "Corrupted Save";
        }
        #endregion

        #region Overrides
        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            if (!IsValid())
                return $"SaveFileInfo [CORRUPTED]: {_fileName}";

            return $"SaveFileInfo: {_fileName} | Player: {_playerName} | Scene: {_currentScene} | " +
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
                return string.Equals(_fileName, other._fileName, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Hash code based on file name
        /// </summary>
        public override int GetHashCode()
        {
            return _fileName?.GetHashCode() ?? 0;
        }
        #endregion
    }
}
