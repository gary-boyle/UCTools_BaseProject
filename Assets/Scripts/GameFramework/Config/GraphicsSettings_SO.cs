using System.Collections.Generic;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GrameFramework.Config
{
    [CreateAssetMenu(fileName = "GraphicsSettings", menuName = "Config Variables/Graphics Settings")]
    public class GraphicsSettings_SO : ConfigCategory
    {
        [Header("Display Settings")]
        public BoolConfigVariable fullscreen = new BoolConfigVariable(
            "graphics.fullscreen", 
            "Fullscreen mode", 
            true, 
            ConfigFlags.Save);
            
        public ResolutionConfigVariable resolution = new ResolutionConfigVariable(
            "graphics.resolution", 
            "Screen resolution", 
            ResolutionOption.FullHD_1920x1080,
            ConfigFlags.Save);

        [Header("Quality Settings")]
        public QualityConfigVariable quality = new QualityConfigVariable(
            "graphics.quality", 
            "Graphics quality level", 
            QualityOption.Medium,
            ConfigFlags.Save);
            
        public BoolConfigVariable vsync = new BoolConfigVariable(
            "graphics.vsync", 
            "Vertical sync", 
            true, 
            ConfigFlags.Save);
        
        // Helper properties
        public int ResolutionWidth => resolution.Width;
        public int ResolutionHeight => resolution.Height;
        public int QualityLevel => quality.QualityLevel;
        public string QualityDisplayName => quality.DisplayName;
        
        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                fullscreen,
                resolution,
                quality,
                vsync
            };
        }
        
        /// <summary>
        /// Reset all graphics settings to their default values
        /// </summary>
        public void ResetToDefaults()
        {
            foreach (var variable in GetAllVariables())
            {
                variable.ResetToDefault();
            }
        }
    }
}
