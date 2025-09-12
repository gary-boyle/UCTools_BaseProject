using System.Collections.Generic;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GameFramework.Config
{
    /// <summary>
    /// Simplified debug settings that just manages data and publishes change events
    /// Debug application logic handled by individual services (ConsoleService, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "DebugSettings", menuName = "Config Variables/Debug Settings")]
    public class DebugSettings_SO : ConfigCategory
    {
        [Header("Debug Display")]
        public BoolConfigVariable showDebugInfo = new BoolConfigVariable(
            "debug.show_debug_info", 
            "Show debug information popup", 
            false, 
            ConfigFlags.Save);
            
        public BoolConfigVariable verboseLogging = new BoolConfigVariable(
            "debug.verbose_logging", 
            "Enable verbose logging", 
            false, 
            ConfigFlags.Save);
            
        public BoolConfigVariable consoleEnabled = new BoolConfigVariable(
            "debug.console_enabled", 
            "Enable debug console", 
            true, 
            ConfigFlags.Save);
        
        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                showDebugInfo,
                verboseLogging,
                consoleEnabled
            };
        }

        /// <summary>
        /// Set debug info display and publish change event
        /// </summary>
        public void SetShowDebugInfo(bool show)
        {
            if (showDebugInfo.Value != show)
            {
                showDebugInfo.Value = show;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set verbose logging and publish change event
        /// </summary>
        public void SetVerboseLogging(bool verbose)
        {
            if (verboseLogging.Value != verbose)
            {
                verboseLogging.Value = verbose;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set console enabled and publish change event
        /// </summary>
        public void SetConsoleEnabled(bool enabled)
        {
            if (consoleEnabled.Value != enabled)
            {
                consoleEnabled.Value = enabled;
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
