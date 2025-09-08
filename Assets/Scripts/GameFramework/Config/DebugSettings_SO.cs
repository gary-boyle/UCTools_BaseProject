using System.Collections.Generic;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GrameFramework.Config
{
    [CreateAssetMenu(fileName = "DebugSettings", menuName = "Config Variables/Debug Settings")]
    public class DebugSettings_SO : ConfigCategory
    {
        [Header("Debug Display")]
        public BoolConfigVariable showFPS = new BoolConfigVariable(
            "debug.show_fps", 
            "Show FPS counter", 
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
                showFPS,
                verboseLogging,
                consoleEnabled
            };
        }

        /// <summary>
        /// Apply FPS display setting
        /// </summary>
        public void SetShowFPS(bool show)
        {
            showFPS.Value = show;
            
            // Enable/disable FPS counter
            // Example: FPSCounter.Instance.SetVisible(show);
            
            Debug.Log($"[DebugSettings] Show FPS: {show}");
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
