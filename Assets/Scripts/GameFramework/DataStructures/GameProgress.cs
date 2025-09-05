using System;
using System.Collections.Generic;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Game progression and achievement data
    /// </summary>
    [Serializable]
    public class GameProgress
    {
        public int score = 0;
        public List<string> completedLevels = new List<string>();
        public List<string> unlockedLevels = new List<string>();
        public Dictionary<string, bool> achievements = new Dictionary<string, bool>();
        public Dictionary<string, float> statistics = new Dictionary<string, float>();

        public static GameProgress CreateDefault()
        {
            var progress = new GameProgress();
            progress.unlockedLevels.Add("GameLevel1"); // Starting level
            return progress;
        }
    }
}
