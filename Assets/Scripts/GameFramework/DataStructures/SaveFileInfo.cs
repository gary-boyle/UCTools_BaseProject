using System;
using GameFramework.DataStructures;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Represents information about a save file for display in the UI
    /// </summary>
    [Serializable]
    public class SaveFileInfo
    {
        public string fileName;
        public string playerName;
        public string difficulty;
        public string currentScene;
        public DateTime lastSaveTime;
        public float totalPlayTimeSeconds;
        public string formattedPlayTime;
        public string formattedDate;
        
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
            
            // Format display strings
            var playTime = TimeSpan.FromSeconds(totalPlayTimeSeconds);
            formattedPlayTime = $"{playTime.Hours:D2}:{playTime.Minutes:D2}:{playTime.Seconds:D2}";
            formattedDate = lastSaveTime.ToString("yyyy-MM-dd HH:mm");
        }
    }
}