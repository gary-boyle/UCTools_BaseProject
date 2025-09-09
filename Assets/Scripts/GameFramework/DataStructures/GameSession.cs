using System;
using System.Collections.Generic;
using GameFramework.StateMachine.Data;
using UnityEngine;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Central game session data - single source of truth for all game state
    /// Replaces the fragmented data structures with a unified, extensible system
    /// </summary>
    [Serializable]
    public class GameSession
    {
        [Header("Session Info")]
        public string playerName = "Player";
        public string difficulty = "Normal";
        public string currentScene = "";
        public DateTime sessionStartTime;
        public DateTime lastSaveTime;
        public float totalPlayTimeSeconds = 0f;
        
        [Header("Player State")]
        public PlayerState player = new PlayerState();
        
        [Header("Game Progress")]
        public GameProgress progress = new GameProgress();
        
        [Header("Custom Data")]
        public Dictionary<string, object> customData = new Dictionary<string, object>();
        
        /// <summary>
        /// Creates a new game session with specified parameters
        /// </summary>
        public static GameSession CreateNewGame(string playerName, string difficulty, string startingScene)
        {
            return new GameSession
            {
                playerName = playerName,
                difficulty = difficulty,
                currentScene = startingScene,
                sessionStartTime = DateTime.Now,
                lastSaveTime = DateTime.Now,
                totalPlayTimeSeconds = 0f,
                player = PlayerState.CreateDefault(difficulty),
                progress = GameProgress.CreateDefault(),
                customData = new Dictionary<string, object>
                {
                    ["creationTime"] = DateTime.Now.ToString(),
                    ["isNewGame"] = true,
                    ["startingPosition"] = "DefaultSpawn"
                }
            };
        }
        
        /// <summary>
        /// Updates play time based on current session
        /// </summary>
        public void UpdatePlayTime()
        {
            totalPlayTimeSeconds = (float)(DateTime.Now - sessionStartTime).TotalSeconds;
        }
        
        /// <summary>
        /// Adjusts the session start time based on already accumulated playtime
        /// Call this after loading a save to ensure correct playtime calculation
        /// </summary>
        public void AdjustSessionStartTimeForLoad()
        {
            // Set session start time to: current time - already accumulated playtime
            sessionStartTime = DateTime.Now.AddSeconds(-totalPlayTimeSeconds);
            Debug.Log($"[GameSession] Adjusted session start time for loaded save. Total playtime: {totalPlayTimeSeconds:F1}s");
        }
        
        /// <summary>
        /// Gets the current playtime without modifying the session
        /// Useful for display purposes
        /// </summary>
        public float GetCurrentPlayTime()
        {
            return (float)(DateTime.Now - sessionStartTime).TotalSeconds;
        }
        
        /// <summary>
        /// Converts session to loading configuration for state transitions
        /// </summary>
        public LoadingConfiguration ToLoadingConfiguration()
        {
            var config = LoadingConfiguration.LoadSave(currentScene, customData);
            config.PlayerName = playerName;
            config.GameData["difficulty"] = difficulty;
            config.GameData["playerLevel"] = player.level;
            config.GameData["totalPlayTime"] = totalPlayTimeSeconds;
            return config;
        }
    }
}
