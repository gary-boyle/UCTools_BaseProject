using System.Collections.Generic;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GrameFramework.Config
{
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
        /// Apply debug info display setting
        /// </summary>
        public void SetShowDebugInfo(bool show)
        {
            showDebugInfo.Value = show;
            Debug.Log($"[DebugSettings] Show debug info: {show}");
        }

        /// <summary>
        /// Apply verbose logging setting
        /// </summary>
        public void SetVerboseLogging(bool verbose)
        {
            verboseLogging.Value = verbose;
            
            // Set logging level
            Debug.unityLogger.logEnabled = verbose;
            
            Debug.Log($"[DebugSettings] Verbose logging: {verbose}");
        }

        /// <summary>
        /// Apply console enabled setting
        /// </summary>
        public void SetConsoleEnabled(bool enabled)
        {
            consoleEnabled.Value = enabled;
            
            // Enable/disable debug console
            // Example: DebugConsole.Instance.SetEnabled(enabled);
            
            Debug.Log($"[DebugSettings] Console enabled: {enabled}");
        }
    }
}
