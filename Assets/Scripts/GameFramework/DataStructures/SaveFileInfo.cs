using System;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Represents information about a save file for display in the UI
    /// Handles display formatting for timestamp-based saves and autosaves
    /// Properly detects autosave files from both filename and session data
    /// </summary>
    [Serializable]
    public class SaveFileInfo
    {
        public string fileName;
        public string displayName; // Formatted name for display
        public string playerName;
        public string difficulty;
        public string currentScene;
        public DateTime lastSaveTime;
        public float totalPlayTimeSeconds;
        public string formattedPlayTime;
        public string formattedDate;
        public bool isAutoSave;
        
        // Additional info for richer display
        public int playerLevel;
        public int score;

        public SaveFileInfo(string fileName, GameSession session)
        {
            this.fileName = fileName;
            this.playerName = session.playerName;
            this.difficulty = session.difficulty;
            this.currentScene = session.currentScene;
            this.lastSaveTime = session.lastSaveTime;
            this.totalPlayTimeSeconds = session.totalPlayTimeSeconds;
            this.playerLevel = session.player.level;
            this.score = session.progress.score;
            
            // Check if this is an autosave from both filename and session data
            this.isAutoSave = DetermineIfAutoSave(fileName, session);
            
            // Generate display name (clean, without autosave indicator)
            this.displayName = GenerateDisplayName();
            
            // Format display strings
            var playTime = TimeSpan.FromSeconds(totalPlayTimeSeconds);
            formattedPlayTime = $"{playTime.Hours:D2}:{playTime.Minutes:D2}:{playTime.Seconds:D2}";
            formattedDate = lastSaveTime.ToString("yyyy-MM-dd HH:mm");
        }

        /// <summary>
        /// Determines if this save file is an autosave by checking both filename and session data
        /// </summary>
        private bool DetermineIfAutoSave(string fileName, GameSession session)
        {
            // Check filename first (most reliable)
            if (fileName.Contains("[AUTOSAVE]"))
            {
                return true;
            }
            
            // Fallback to session data if available
            if (session.customData.ContainsKey("isAutoSave"))
            {
                if (bool.TryParse(session.customData["isAutoSave"].ToString(), out bool sessionAutoSave))
                {
                    return sessionAutoSave;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Generates a clean display name without autosave indicators (those are shown separately)
        /// </summary>
        private string GenerateDisplayName()
        {
            var parts = fileName.Split('_');
            
            // For autosaves: PlayerName_[AUTOSAVE]_yyyy-MM-dd_HH-mm-ss
            // For regular saves: PlayerName_Save_yyyy-MM-dd_HH-mm-ss
            if (parts.Length >= 3)
            {
                var dateTime = string.Join("_", parts[parts.Length - 2], parts[parts.Length - 1]);
                var saveType = isAutoSave ? "AutoSave" : "Save";
                return $"{playerName} {saveType} {dateTime}";
            }
            
            // Fallback to original filename
            return fileName;
        }
        
        /// <summary>
        /// Gets a short description for the save type
        /// </summary>
        public string GetSaveTypeDescription()
        {
            return isAutoSave ? "Automatic Save" : "Manual Save";
        }
        
        /// <summary>
        /// Gets a user-friendly save type indicator
        /// </summary>
        public string GetSaveTypeIndicator()
        {
            return isAutoSave ? "[AUTO]" : "[SAVE]";
        }
    }
}
