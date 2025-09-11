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

        public SaveFileInfo(string fileName, GameSession session)
        {
            this.fileName = fileName;
            this.playerName = session.playerName;
            this.difficulty = session.difficulty;
            this.currentScene = session.currentScene;
            this.lastSaveTime = session.lastSaveTime;
            this.playerLevel = session.player.level;
            this.score = session.progress.score;
    
            // Use SAVED playtime information for save file display
            this.formattedPlayTime = session.SavedFormattedPlayTime;     
            this.formattedSessionTime = session.SavedFormattedSessionTime; 
            
            // Check if this is an autosave from both filename and session data
            this.isAutoSave = session.WasAutoSave;
    
            // Format display strings
            formattedDate = lastSaveTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
