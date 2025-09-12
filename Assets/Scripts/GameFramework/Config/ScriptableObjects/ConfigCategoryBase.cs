using System.Collections.Generic;
using UnityEngine;
using GameFramework.Config.Enums;
using GameFramework.Config.Variables;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;

namespace GameFramework.Config.ScriptableObjects
{
    /// <summary>
    /// Base class for configuration category ScriptableObjects
    /// Provides common functionality for all config categories
    /// </summary>
    public abstract class ConfigCategoryBase : ScriptableObject
    {
        public abstract ConfigTypes CategoryType { get; }
        
        /// <summary>Get all config variables in this category</summary>
        public abstract List<ConfigVariableBase> GetAllVariables();
        
        /// <summary>Reset all variables in this category to defaults</summary>
        public virtual void ResetToDefaults()
        {
            foreach (var variable in GetAllVariables())
            {
                variable.ResetToDefault();
            }
        }
        
        /// <summary>Validate all variables in this category</summary>
        public virtual bool ValidateAll(out List<string> errors)
        {
            errors = new List<string>();
            bool allValid = true;
            
            foreach (var variable in GetAllVariables())
            {
                if (!variable.ValidateConfiguration(out string error))
                {
                    errors.Add($"{variable.name}: {error}");
                    allValid = false;
                }
            }
            
            return allValid;
        }
        
        protected virtual void OnValidate()
        {
            if (ValidateAll(out var errors))
            {
                // All good
            }
            else
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"Config validation error in {name}: {error}", this);
                }
            }
        }
        
        /// <summary>
        /// Enhanced event publishing that also auto-saves all settings
        /// </summary>
        protected async void PublishOptionsChangedEvent()
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new OptionsChangedEvent());
            
            // Auto-save all settings when any setting changes
            await SettingsRegistry.SaveAllSettingsAsync();
        }
        
    }
}