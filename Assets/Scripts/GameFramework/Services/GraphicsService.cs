using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Graphics service that handles all Unity graphics API calls
    /// Responds to configuration changes via events
    /// 
    /// INTENT: Centralize all graphics settings application
    /// DESIGN: Event-driven graphics management, single responsibility
    /// PROS: Clean separation, all graphics logic in one place, easily testable
    /// CONS: Additional service complexity
    /// </summary>
    public class GraphicsService : IGraphicsService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly IConfigService _configService;

        /// <summary>
        /// Constructor injection
        /// </summary>
        public GraphicsService(IEventSystem eventSystem, IConfigService configService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            // Subscribe to config changes
            _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);
            
            // Apply initial graphics settings
            ApplyGraphicsSettings();
            
            IsInitialized = true;
            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            _eventSystem.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
            IsInitialized = false;
        }

        /// <summary>
        /// Handle options changed events
        /// </summary>
        private void OnOptionsChanged(OptionsChangedEvent evt)
        {
            ApplyGraphicsSettings();
        }

        /// <summary>
        /// Apply all current graphics settings from config
        /// </summary>
        private void ApplyGraphicsSettings()
        {
            try
            {
                var fullscreen = _configService.GetConfigValue<bool>("graphics.fullscreen");
                var resolution = _configService.GetConfigValue<ResolutionOption>("graphics.resolution");
                var quality = _configService.GetConfigValue<QualityOption>("graphics.quality");
                var vsync = _configService.GetConfigValue<bool>("graphics.vsync");

                ApplyResolution(resolution, fullscreen);
                ApplyQuality(quality);
                ApplyVSync(vsync);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GraphicsService] Error applying graphics settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply resolution and fullscreen mode
        /// </summary>
        private void ApplyResolution(ResolutionOption resolutionOption, bool fullscreen)
        {
            try
            {
                var (width, height) = resolutionOption.GetResolution();
                
                // Check if we need to change resolution
                if (Screen.width != width || Screen.height != height || Screen.fullScreen != fullscreen)
                {
                    Screen.SetResolution(width, height, fullscreen);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GraphicsService] Error setting resolution: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply quality settings
        /// </summary>
        private void ApplyQuality(QualityOption qualityOption)
        {
            try
            {
                int qualityLevel = GetQualityLevel(qualityOption);
                
                if (QualitySettings.GetQualityLevel() != qualityLevel)
                {
                    QualitySettings.SetQualityLevel(qualityLevel);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GraphicsService] Error setting quality: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply VSync settings
        /// </summary>
        private void ApplyVSync(bool enableVSync)
        {
            try
            {
                int vSyncCount = enableVSync ? 1 : 0;
                
                if (QualitySettings.vSyncCount != vSyncCount)
                {
                    QualitySettings.vSyncCount = vSyncCount;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GraphicsService] Error setting VSync: {ex.Message}");
            }
        }

        /// <summary>
        /// Convert quality option to Unity quality level
        /// </summary>
        private int GetQualityLevel(QualityOption qualityOption)
        {
            return qualityOption switch
            {
                QualityOption.Low => 0,
                QualityOption.Medium => 1,
                QualityOption.High => 2,
                QualityOption.VeryHigh => 3,
                _ => 2 // Default to High
            };
        }

        /// <summary>
        /// Get current screen resolution info
        /// </summary>
        public (int width, int height, bool fullscreen) GetCurrentResolution()
        {
            return (Screen.width, Screen.height, Screen.fullScreen);
        }

        /// <summary>
        /// Get current quality level
        /// </summary>
        public int GetCurrentQualityLevel()
        {
            return QualitySettings.GetQualityLevel();
        }

        /// <summary>
        /// Get current VSync state
        /// </summary>
        public bool GetCurrentVSyncEnabled()
        {
            return QualitySettings.vSyncCount > 0;
        }

        /// <summary>
        /// Check if resolution is supported
        /// </summary>
        public bool IsResolutionSupported(int width, int height)
        {
            foreach (var supportedResolution in Screen.resolutions)
            {
                if (supportedResolution.width == width && supportedResolution.height == height)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
