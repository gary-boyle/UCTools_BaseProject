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
    /// Simplified graphics settings that just manages data and publishes change events
    /// All graphics application logic moved to GraphicsService
    /// </summary>
    [CreateAssetMenu(fileName = "GraphicsSettings", menuName = "Game Framework/Config Variables/Graphics Settings")]
    public class GraphicsSettings_SO : ConfigCategoryBase
    {
        [Header("Display Settings")]
        public BoolConfigVariable Fullscreen = new BoolConfigVariable(
            "graphics.fullscreen", 
            "Fullscreen mode", 
            true, 
            ConfigFlags.Save);
            
        public ResolutionConfigVariable Resolution = new ResolutionConfigVariable(
            "graphics.resolution", 
            "Screen resolution", 
            ResolutionOption.FullHD_1920x1080,
            ConfigFlags.Save);

        [Header("Quality Settings")]
        public QualityConfigVariable Quality = new QualityConfigVariable(
            "graphics.quality", 
            "Graphics quality level", 
            QualityOption.Medium,
            ConfigFlags.Save);
            
        public BoolConfigVariable Vsync = new BoolConfigVariable(
            "graphics.vsync", 
            "Vertical sync", 
            true, 
            ConfigFlags.Save);
        
        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                Fullscreen,
                Resolution,
                Quality,
                Vsync
            };
        }
        
        public override ConfigTypes CategoryType => ConfigTypes.Graphics;

        /// <summary>
        /// Set fullscreen and publish change event
        /// </summary>
        public void SetFullscreen(bool isFullscreen)
        {
            if (Fullscreen.Value != isFullscreen)
            {
                Fullscreen.Value = isFullscreen;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set VSync and publish change event
        /// </summary>
        public void SetVSync(bool enableVSync)
        {
            if (Vsync.Value != enableVSync)
            {
                Vsync.Value = enableVSync;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set quality and publish change event
        /// </summary>
        public void SetQuality(QualityOption qualityOption)
        {
            if (Quality.Value != qualityOption)
            {
                Quality.Value = qualityOption;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set resolution and publish change event
        /// </summary>
        public void SetResolution(ResolutionOption resolutionOption)
        {
            if (Resolution.Value != resolutionOption)
            {
                Resolution.Value = resolutionOption;
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
            return (int)Quality.Value;
        }

        /// <summary>
        /// Get current resolution as index for UI dropdowns
        /// </summary>
        public int GetResolutionIndex()
        {
            return (int)Resolution.Value;
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
