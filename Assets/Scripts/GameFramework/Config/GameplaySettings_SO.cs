using System.Collections.Generic;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GrameFramework.Config
{
    [CreateAssetMenu(fileName = "GameplaySettings", menuName = "Config Variables/Gameplay Settings")]
    public class GameplaySettings_SO : ConfigCategory
    {
        [Header("Difficulty")]
        public IntConfigVariable difficulty = new IntConfigVariable(
            "game.difficulty", 
            "Game difficulty (0 = easy, 1 = normal, 2 = hard)", 
            1, 
            ConfigFlags.Save,
            minValue: 0,
            maxValue: 2);

        [Header("Save System")]
        public BoolConfigVariable autoSave = new BoolConfigVariable(
            "game.auto_save", 
            "Auto-save enabled", 
            true, 
            ConfigFlags.Save);
            
        public IntConfigVariable autoSaveInterval = new IntConfigVariable(
            "game.auto_save_interval", 
            "Auto-save interval in seconds", 
            300, 
            ConfigFlags.Save,
            minValue: 30,
            maxValue: 3600);
        
        private readonly string[] _difficultyNames = { "Easy", "Normal", "Hard" };
        
        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                difficulty,
                autoSave,
                autoSaveInterval
            };
        }

        /// <summary>
        /// Apply difficulty setting with game logic
        /// </summary>
        public void SetDifficulty(int difficultyLevel)
        {
            difficultyLevel = Mathf.Clamp(difficultyLevel, 0, 2);
            difficulty.Value = difficultyLevel;
            
            // Apply difficulty-specific game logic here
            ApplyDifficultySettings(difficultyLevel);
            
            Debug.Log($"[GameplaySettings] Difficulty: {_difficultyNames[difficultyLevel]}");
        }

        /// <summary>
        /// Apply auto-save setting
        /// </summary>
        public void SetAutoSave(bool enabled)
        {
            autoSave.Value = enabled;
            
            // Enable/disable auto-save system
            // Example: AutoSaveManager.Instance.SetEnabled(enabled);
            
            Debug.Log($"[GameplaySettings] Auto-save: {enabled}");
        }

        /// <summary>
        /// Apply auto-save interval setting
        /// </summary>
        public void SetAutoSaveInterval(int intervalSeconds)
        {
            intervalSeconds = Mathf.Clamp(intervalSeconds, 30, 3600);
            autoSaveInterval.Value = intervalSeconds;
            
            // Update auto-save timer
            // Example: AutoSaveManager.Instance.SetInterval(intervalSeconds);
            
            Debug.Log($"[GameplaySettings] Auto-save interval: {intervalSeconds}s");
        }

        /// <summary>
        /// Get difficulty choices for UI display
        /// </summary>
        public string[] GetDifficultyChoices()
        {
            return _difficultyNames;
        }

        /// <summary>
        /// Apply difficulty-specific settings to game systems
        /// </summary>
        private void ApplyDifficultySettings(int difficultyLevel)
        {
            switch (difficultyLevel)
            {
                case 0: // Easy
                    // Example: EnemyManager.SetDamageMultiplier(0.7f);
                    // Example: PlayerManager.SetHealthMultiplier(1.5f);
                    break;
                case 1: // Normal
                    // Example: EnemyManager.SetDamageMultiplier(1.0f);
                    // Example: PlayerManager.SetHealthMultiplier(1.0f);
                    break;
                case 2: // Hard
                    // Example: EnemyManager.SetDamageMultiplier(1.5f);
                    // Example: PlayerManager.SetHealthMultiplier(0.7f);
                    break;
            }
        }
    }
}
