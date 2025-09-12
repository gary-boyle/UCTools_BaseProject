using System.Collections.Generic;
using GameFramework.Config.Enums;
using GameFramework.Config.Variables;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameFramework.Config.ScriptableObjects
{
    /// <summary>
    /// Simplified gameplay settings that just manages data and publishes change events
    /// All gameplay application logic moved to relevant services (SaveService, GameplayService, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "GameplaySettings", menuName = "Game Framework/Config Variables/Gameplay Settings")]
    public class GameplaySettings_SO : ConfigCategoryBase
    {
        [Header("Difficulty")]
        public IntConfigVariable Difficulty = new IntConfigVariable(
            "game.difficulty", 
            "Game difficulty (0 = easy, 1 = normal, 2 = hard)", 
            1, 
            ConfigFlags.Save,
            minValue: 0,
            maxValue: 2);

        [Header("Save System")]
        public BoolConfigVariable AutoSave = new BoolConfigVariable(
            "game.auto_save", 
            "Auto-save enabled", 
            true, 
            ConfigFlags.Save);
            
        public IntConfigVariable AutoSaveInterval = new IntConfigVariable(
            "game.auto_save_interval", 
            "Auto-save interval in minutes", 
            5, 
            ConfigFlags.Save,
            minValue: 1,
            maxValue: 60);
        
        public override ConfigTypes CategoryType => ConfigTypes.Gameplay;

        private readonly string[] _difficultyNames = { "Easy", "Normal", "Hard" };
        
        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                Difficulty,
                AutoSave,
                AutoSaveInterval
            };
        }

        /// <summary>
        /// Set difficulty and publish change event
        /// </summary>
        public void SetDifficulty(int difficultyLevel)
        {
            difficultyLevel = Mathf.Clamp(difficultyLevel, 0, 2);
            if (Difficulty.Value != difficultyLevel)
            {
                Difficulty.Value = difficultyLevel;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set auto-save enabled and publish change event
        /// </summary>
        public void SetAutoSave(bool enabled)
        {
            if (AutoSave.Value != enabled)
            {
                AutoSave.Value = enabled;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set auto-save interval (in minutes) and publish change event
        /// </summary>
        public void SetAutoSaveInterval(int intervalMinutes)
        {
            intervalMinutes = Mathf.Clamp(intervalMinutes, 1, 60);
            if (AutoSaveInterval.Value != intervalMinutes)
            {
                AutoSaveInterval.Value = intervalMinutes;
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
            return Difficulty.Value;
        }

        /// <summary>
        /// Get current difficulty name for display
        /// </summary>
        public string GetDifficultyName()
        {
            int index = Mathf.Clamp(Difficulty.Value, 0, _difficultyNames.Length - 1);
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
            return AutoSaveInterval.Value * 60;
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
