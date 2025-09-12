using System.Collections.Generic;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GameFramework.Config
{
    /// <summary>
    /// Simplified gameplay settings that just manages data and publishes change events
    /// All gameplay application logic moved to relevant services (SaveService, GameplayService, etc.)
    /// </summary>
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
            "Auto-save interval in minutes", 
            5, 
            ConfigFlags.Save,
            minValue: 1,
            maxValue: 60);
        
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
        /// Set difficulty and publish change event
        /// </summary>
        public void SetDifficulty(int difficultyLevel)
        {
            difficultyLevel = Mathf.Clamp(difficultyLevel, 0, 2);
            if (difficulty.Value != difficultyLevel)
            {
                difficulty.Value = difficultyLevel;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set auto-save enabled and publish change event
        /// </summary>
        public void SetAutoSave(bool enabled)
        {
            if (autoSave.Value != enabled)
            {
                autoSave.Value = enabled;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set auto-save interval (in minutes) and publish change event
        /// </summary>
        public void SetAutoSaveInterval(int intervalMinutes)
        {
            intervalMinutes = Mathf.Clamp(intervalMinutes, 1, 60);
            if (autoSaveInterval.Value != intervalMinutes)
            {
                autoSaveInterval.Value = intervalMinutes;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Publish options changed event to notify relevant services
        /// </summary>
        private void PublishOptionsChangedEvent()
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new OptionsChangedEvent());
        }

        /// <summary>
        /// Get difficulty choices for UI display
        /// </summary>
        public string[] GetDifficultyChoices()
        {
            return _difficultyNames;
        }

        /// <summary>
        /// Get current difficulty as index for UI dropdowns
        /// </summary>
        public int GetDifficultyIndex()
        {
            return difficulty.Value;
        }

        /// <summary>
        /// Get current difficulty name for display
        /// </summary>
        public string GetDifficultyName()
        {
            int index = Mathf.Clamp(difficulty.Value, 0, _difficultyNames.Length - 1);
            return _difficultyNames[index];
        }

        /// <summary>
        /// Set difficulty from dropdown index
        /// </summary>
        public void SetDifficultyFromIndex(int index)
        {
            if (index >= 0 && index < _difficultyNames.Length)
            {
                SetDifficulty(index);
            }
        }

        /// <summary>
        /// Get auto-save interval in seconds for services that need it
        /// </summary>
        public int GetAutoSaveIntervalInSeconds()
        {
            return autoSaveInterval.Value * 60;
        }

        /// <summary>
        /// Set auto-save interval from seconds (for backward compatibility)
        /// </summary>
        public void SetAutoSaveIntervalFromSeconds(int seconds)
        {
            int minutes = Mathf.Max(1, seconds / 60);
            SetAutoSaveInterval(minutes);
        }
    }
}
