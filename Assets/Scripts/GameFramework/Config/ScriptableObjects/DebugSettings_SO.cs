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
    /// Simplified debug settings that just manages data and publishes change events
    /// Debug application logic handled by individual services (ConsoleService, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "DebugSettings", menuName = "Config Variables/Debug Settings")]
    public class DebugSettings_SO : ConfigCategoryBase
    {
        [Header("Debug Display")]
        public BoolConfigVariable ShowDebugInfo = new BoolConfigVariable(
            "debug.show_debug_info", 
            "Show debug information popup", 
            false, 
            ConfigFlags.Save);
            
        public BoolConfigVariable VerboseLogging = new BoolConfigVariable(
            "debug.verbose_logging", 
            "Enable verbose logging", 
            false, 
            ConfigFlags.Save);
            
        public BoolConfigVariable ConsoleEnabled = new BoolConfigVariable(
            "debug.console_enabled", 
            "Enable debug console", 
            true, 
            ConfigFlags.Save);
        
        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                ShowDebugInfo,
                VerboseLogging,
                ConsoleEnabled
            };
        }
        public override ConfigTypes CategoryType => ConfigTypes.Debug;

        /// <summary>
        /// Set debug info display and publish change event
        /// </summary>
        public void SetShowDebugInfo(bool show)
        {
            if (ShowDebugInfo.Value != show)
            {
                ShowDebugInfo.Value = show;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set verbose logging and publish change event
        /// </summary>
        public void SetVerboseLogging(bool verbose)
        {
            if (VerboseLogging.Value != verbose)
            {
                VerboseLogging.Value = verbose;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set console enabled and publish change event
        /// </summary>
        public void SetConsoleEnabled(bool enabled)
        {
            if (ConsoleEnabled.Value != enabled)
            {
                ConsoleEnabled.Value = enabled;
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
    }
}
