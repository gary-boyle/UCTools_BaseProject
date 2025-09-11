using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Game progression and achievement data
    /// </summary>
    [Serializable]
    public class GameProgress
    {
        public int Score = 0;
        public List<string> CompletedLevels;
        public List<string> UnlockedLevels;
        public Dictionary<string, bool> Achievements;
        public Dictionary<string, float> Statistics;

        public static GameProgress CreateDefault()
        {
            var progress = new GameProgress();
            progress.UnlockedLevels.Add("GameLevel1");
            return progress;
        }
    }
}
