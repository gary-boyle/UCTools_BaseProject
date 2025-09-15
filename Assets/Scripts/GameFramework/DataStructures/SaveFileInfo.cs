using System;
using GameFramework.Utilities;

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
        public string FormattedDate;
        public bool IsAutoSave;
        public int PlayerLevel;
        public int Score;

        public SaveFileInfo(string fileName, GameSessionData sessionData)
        {
            FileName = fileName;
            PlayerName = sessionData.PlayerName;
            Difficulty = sessionData.Difficulty;
            CurrentScene = sessionData.CurrentScene;
            LastSaveTime = sessionData.LastSaveTime;
    
            // Use SAVED playtime information for save file display
            FormattedPlayTime = TimeUtilities.FormatTimeFromSeconds(sessionData?.SavedGameTime ?? 0f);
            
            // Check if this is an autosave from both filename and session data
            IsAutoSave = sessionData.WasAutoSave;
    
            // Format display strings
            FormattedDate = LastSaveTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}