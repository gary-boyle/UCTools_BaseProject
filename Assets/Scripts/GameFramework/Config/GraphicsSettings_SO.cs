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
        /// Apply fullscreen setting with immediate effect
        /// </summary>
        public void SetFullscreen(bool isFullscreen)
        {
            fullscreen.Value = isFullscreen;
            
            var (width, height) = resolution.Value.GetResolution();
            Screen.SetResolution(width, height, isFullscreen);
            
            Debug.Log($"[GraphicsSettings] Fullscreen: {isFullscreen}");
        }

        /// <summary>
        /// Apply VSync setting with immediate effect
        /// </summary>
        public void SetVSync(bool enableVSync)
        {
            vsync.Value = enableVSync;
            QualitySettings.vSyncCount = enableVSync ? 1 : 0;
            
            Debug.Log($"[GraphicsSettings] VSync: {enableVSync}");
        }

        /// <summary>
        /// Apply quality setting with immediate effect
        /// </summary>
        public void SetQuality(QualityOption qualityOption)
        {
            quality.Value = qualityOption;
            QualitySettings.SetQualityLevel(quality.QualityLevel);
            
            Debug.Log($"[GraphicsSettings] Quality: {quality.DisplayName}");
        }

        /// <summary>
        /// Apply resolution setting with immediate effect
        /// </summary>
        public void SetResolution(ResolutionOption resolutionOption)
        {
            resolution.Value = resolutionOption;
            
            var (width, height) = resolutionOption.GetResolution();
            Screen.SetResolution(width, height, fullscreen.Value);
            
            Debug.Log($"[GraphicsSettings] Resolution: {width}x{height}");
        }

        /// <summary>
        /// Get available quality options for UI display
        /// </summary>
        public string[] GetQualityChoices()
        {
            return QualityOptionExtensions.GetAllDisplayNames();
        }

        /// <summary>
        /// Get available resolution options for UI display
        /// </summary>
        public string[] GetResolutionChoices()
        {
            return ResolutionOptionExtensions.GetAllDisplayNames();
        }
    }
}
