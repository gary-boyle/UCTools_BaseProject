using System;
using UnityEngine.Serialization;

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
        public string FileName;
        public string PlayerName;
        public string Difficulty;
        public string CurrentScene;
        public DateTime LastSaveTime;
        public string FormattedPlayTime;
        public string FormattedSessionTime;
        public string FormattedDate;
        public bool IsAutoSave;
        public int PlayerLevel;
        public int Score;

        public SaveFileInfo(string fileName, GameSession session)
        {
            FileName = fileName;
            PlayerName = session.playerName;
            Difficulty = session.difficulty;
            CurrentScene = session.currentScene;
            LastSaveTime = session.lastSaveTime;
            PlayerLevel = session.player.Level;
            Score = session.progress.Score;
    
            // Use SAVED playtime information for save file display
            FormattedPlayTime = session.SavedFormattedPlayTime;     
            FormattedSessionTime = session.SavedFormattedSessionTime; 
            
            // Check if this is an autosave from both filename and session data
            IsAutoSave = session.WasAutoSave;
    
            // Format display strings
            FormattedDate = LastSaveTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
