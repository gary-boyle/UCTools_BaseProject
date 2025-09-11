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
        public List<string> CompletedLevels = new List<string>();
        public List<string> UnlockedLevels = new List<string>();
        public Dictionary<string, bool> Achievements = new Dictionary<string, bool>();
        public Dictionary<string, float> Statistics = new Dictionary<string, float>();

        public static GameProgress CreateDefault()
        {
            var progress = new GameProgress();
            progress.UnlockedLevels.Add("GameLevel1");
            return progress;
        }
    }
}
