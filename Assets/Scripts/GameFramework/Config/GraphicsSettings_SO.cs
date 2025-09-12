using System.Collections.Generic;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GameFramework.Config
{
    /// <summary>
    /// Simplified graphics settings that just manages data and publishes change events
    /// All graphics application logic moved to GraphicsService
    /// </summary>
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
        /// Set fullscreen and publish change event
        /// </summary>
        public void SetFullscreen(bool isFullscreen)
        {
            if (fullscreen.Value != isFullscreen)
            {
                fullscreen.Value = isFullscreen;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set VSync and publish change event
        /// </summary>
        public void SetVSync(bool enableVSync)
        {
            if (vsync.Value != enableVSync)
            {
                vsync.Value = enableVSync;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set quality and publish change event
        /// </summary>
        public void SetQuality(QualityOption qualityOption)
        {
            if (quality.Value != qualityOption)
            {
                quality.Value = qualityOption;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set resolution and publish change event
        /// </summary>
        public void SetResolution(ResolutionOption resolutionOption)
        {
            if (resolution.Value != resolutionOption)
            {
                resolution.Value = resolutionOption;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Publish options changed event to notify GraphicsService
        /// </summary>
        private void PublishOptionsChangedEvent()
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new OptionsChangedEvent());
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

        /// <summary>
        /// Get current quality as index for UI dropdowns
        /// </summary>
        public int GetQualityIndex()
        {
            return (int)quality.Value;
        }

        /// <summary>
        /// Get current resolution as index for UI dropdowns
        /// </summary>
        public int GetResolutionIndex()
        {
            return (int)resolution.Value;
        }

        /// <summary>
        /// Set quality from dropdown index
        /// </summary>
        public void SetQualityFromIndex(int index)
        {
            if (System.Enum.IsDefined(typeof(QualityOption), index))
            {
                SetQuality((QualityOption)index);
            }
        }

        /// <summary>
        /// Set resolution from dropdown index
        /// </summary>
        public void SetResolutionFromIndex(int index)
        {
            if (System.Enum.IsDefined(typeof(ResolutionOption), index))
            {
                SetResolution((ResolutionOption)index);
            }
        }
    }
}
