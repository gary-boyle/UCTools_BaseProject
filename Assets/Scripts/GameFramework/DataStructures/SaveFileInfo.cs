using System;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Represents information about a save file for display in the UI
    /// Handles display formatting for timestamp-based saves and autosaves
    /// Properly detects autosave files from both filename and session data
    /// Uses TimeService-based playtime information for accurate display
    /// </summary>
    [Serializable]
    public class SaveFileInfo
    {
        public string fileName;
        public string playerName;
        public string difficulty;
        public string currentScene;
        public DateTime lastSaveTime;
        public string formattedPlayTime;
        public string formattedSessionTime;
        public string formattedDate;
        public bool isAutoSave;
        
        // Additional info for richer display
        public int playerLevel;
        public int score;
        public PlayTimeInfo playTimeInfo;

        public SaveFileInfo(string fileName, GameSession session)
        {
            this.fileName = fileName;
            this.playerName = session.playerName;
            this.difficulty = session.difficulty;
            this.currentScene = session.currentScene;
            this.lastSaveTime = session.lastSaveTime;
            this.playerLevel = session.player.level;
            this.score = session.progress.score;
            
            // Use TimeService-based playtime information
            this.formattedPlayTime = session.FormattedPlayTime;
            this.formattedSessionTime = session.FormattedSessionTime;
            this.playTimeInfo = session.GetPlayTimeInfo();
            
            // Check if this is an autosave from both filename and session data
            this.isAutoSave = DetermineIfAutoSave(fileName, session);
            
            // Format display strings
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
        /// Gets a short description for the save type
        /// </summary>
        public string GetSaveTypeDescription()
        {
            return isAutoSave ? "Automatic Save" : "Manual Save";
        }
    }
}
