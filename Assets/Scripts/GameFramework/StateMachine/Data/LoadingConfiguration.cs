using System;
using System.Collections.Generic;
using GameFramework.StateMachine.Enum;

namespace GameFramework.StateMachine.Data
{
    /// <summary>
    /// Configuration data for different loading scenarios
    /// </summary>
    [Serializable]
    public class LoadingConfiguration
    {
        public LoadingType Type { get; set; }
        public string SceneName { get; set; }
        public string PlayerName { get; set; }
        public Dictionary<string, object> GameData { get; set; } = new();
        public bool ShowLoadingScreen { get; set; } = true;
        public float MinimumLoadingTime { get; set; } = 1f; // For UX purposes
        
        public static LoadingConfiguration NewGame(string sceneName, string playerName = "Player")
        {
            return new LoadingConfiguration
            {
                Type = LoadingType.NewGame,
                SceneName = sceneName,
                PlayerName = playerName,
                GameData = new Dictionary<string, object>
                {
                    ["isNewGame"] = true,
                    ["playerLevel"] = 1,
                    ["startingPosition"] = "DefaultSpawn"
                }
            };
        }
        
        public static LoadingConfiguration LoadSave(string sceneName, Dictionary<string, object> saveData)
        {
            return new LoadingConfiguration
            {
                Type = LoadingType.LoadSave,
                SceneName = sceneName,
                PlayerName = saveData.ContainsKey("playerName") ? saveData["playerName"].ToString() : "Player",
                GameData = saveData
            };
        }
        
        public static LoadingConfiguration SceneTransition(string sceneName, Dictionary<string, object> transitionData = null)
        {
            return new LoadingConfiguration
            {
                Type = LoadingType.SceneTransition,
                SceneName = sceneName,
                GameData = transitionData ?? new Dictionary<string, object>(),
                MinimumLoadingTime = 0.5f // Shorter for scene transitions
            };
        }
    }
    

}
