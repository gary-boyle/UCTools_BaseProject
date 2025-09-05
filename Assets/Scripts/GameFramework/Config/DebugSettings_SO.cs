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
    }
}