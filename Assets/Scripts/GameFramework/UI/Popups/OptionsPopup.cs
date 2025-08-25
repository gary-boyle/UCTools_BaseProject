using System;
using System.Linq;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using UCTools_ConfigVariables;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Popups
{
    /// <summary>
    /// Options popup implementation with ConfigVar integration
    /// </summary>
    public class OptionsPopup : UIPopup
    {
        // Audio
        private Toggle _toggleAudio;
        private SliderInt _masterVolumeSlider;
        private SliderInt _musicVolumeSlider;
        private SliderInt _sfxVolumeSlider;
        private Button _resetDefaultsButton;
        
        // Graphics
        private Toggle _toggleFullscreen;
        private Toggle _toggleVSync;
        private DropdownField _qualityDropdown;
        private DropdownField _resolutionDropdown;
        
        private Button _closeButton;
        
        private readonly IConfigService _configService;
        private readonly IUIService _uiService;
        
        public OptionsPopup(VisualElement rootElement) : base(rootElement)
        {
            _configService = GameManager.GetService<IConfigService>() ?? throw new ArgumentNullException(nameof(_configService));
            _uiService = GameManager.GetService<IUIService>() ?? throw new ArgumentNullException(nameof(_uiService));
            
            InitializeControls();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        private void InitializeControls()
        {
            // Audio
            _toggleAudio = RootElement?.Q<Toggle>("tgl_EnableAudio");
            _masterVolumeSlider = RootElement?.Q<SliderInt>("slider_MasterVolume");
            _musicVolumeSlider = RootElement?.Q<SliderInt>("slider_MusicVolume");
            _sfxVolumeSlider = RootElement?.Q<SliderInt>("slider_SFXVolume");
            _resetDefaultsButton = RootElement?.Q<Button>("btn_Reset");
            
            // Graphics
            _toggleFullscreen= RootElement.Q<Toggle>("tgl_FullScreen");
            _toggleVSync = RootElement.Q<Toggle>("tgl_Vsync");
            _qualityDropdown = RootElement.Q<DropdownField>("dd_Quality"); 
            _resolutionDropdown = RootElement.Q<DropdownField>("dd_Resolution");
            
            _closeButton = RootElement?.Q<Button>("btn_Close");

            SetupDropdowns();
            
            // Register callbacks
            // Audio
            _toggleAudio?.RegisterCallback<ChangeEvent<bool>>(OnToggleAudioChanged);
            _masterVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnMasterVolumeChanged);
            _musicVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnMusicVolumeChanged);
            _sfxVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnSfxVolumeChanged);
            
            // Graphics
            _toggleFullscreen?.RegisterCallback<ChangeEvent<bool>>(OnToggleFullscreenChanged);
            _toggleVSync?.RegisterCallback<ChangeEvent<bool>>(OnToggleVsyncChanged);
            _qualityDropdown.RegisterValueChangedCallback(OnQualityChanged);
            _resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);

            // Misc
            _resetDefaultsButton?.RegisterCallback<ClickEvent>(OnResetDefaultsClicked);
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseClicked);
        }

        private void SetupDropdowns()
        {
            _qualityDropdown.choices = QualityOptionExtensions.GetAllDisplayNames().ToList();
            _resolutionDropdown.choices = ResolutionOptionExtensions.GetAllDisplayNames().ToList();
        }
    
        private void OnQualityChanged(ChangeEvent<string> evt)
        {
            // Convert display name back to enum
            var displayNames = QualityOptionExtensions.GetAllDisplayNames();
            int selectedIndex = Array.IndexOf(displayNames, evt.newValue);
        
            if (selectedIndex >= 0)
            {
                // Update the ScriptableObject directly
                QualityOption newQuality = (QualityOption)selectedIndex;
                _configService?.SetConfigValue("graphics.quality", newQuality);
                
                // TODO Apply the resolution change
            }
        }
        
        private void OnResolutionChanged(ChangeEvent<string> evt)
        {
            // Convert display name back to enum
            var displayNames = ResolutionOptionExtensions.GetAllDisplayNames();
            int selectedIndex = Array.IndexOf(displayNames, evt.newValue);
        
            if (selectedIndex >= 0)
            {
                // Update the ScriptableObject directly
                ResolutionOption newResolution = (ResolutionOption)selectedIndex;
                _configService?.SetConfigValue("graphics.resolution", newResolution);
                
                // Get the actual resolution values and apply
                var (width, height) = newResolution.GetResolution();
                Screen.SetResolution(width, height, Screen.fullScreen);
            }
        }

        protected override void OnShow()
        {
            // Populate UI with current values from ScriptableObject
            RefreshUI();
        }
        
        private void RefreshUI()
        {
            _qualityDropdown.index = (int)_configService.GetConfigValue<QualityOption>("graphics.quality");
            _resolutionDropdown.index = (int)_configService.GetConfigValue<ResolutionOption>("graphics.resolution");
        }
        
        private void OnToggleFullscreenChanged(ChangeEvent<bool> evt)
        {
            // _configService?.SetConfigValue("graphics.fullscreen", evt.newValue);
        }
        
        private void OnToggleVsyncChanged(ChangeEvent<bool> evt)
        {
            //_configService?.SetConfigValue("graphics.vsync", evt.newValue);
        }
        
        private void OnToggleAudioChanged(ChangeEvent<bool> evt)
        {
            //_configService?.SetConfigValue("audio.enabled", evt.newValue);
        }
        
        private void OnMasterVolumeChanged(ChangeEvent<int> evt)
        {
            //_configService?.SetConfigValue("audio.master_volume", evt.newValue);
        }
        
        private void OnMusicVolumeChanged(ChangeEvent<int> evt)
        {
            //_configService?.SetConfigValue("audio.music_volume", evt.newValue);
        }
        
        private void OnSfxVolumeChanged(ChangeEvent<int> evt)
        {
            //_configService?.SetConfigValue("audio.sfx_volume", evt.newValue);
        }
        
        private void OnResetDefaultsClicked(ClickEvent evt)
        {
            // Reset other settings through config service
            _configService?.ResetToDefaults();
            
            // Refresh UI to show reset values
            RefreshUI();
        }
        
        private async void OnCloseClicked(ClickEvent evt)
        {
            Debug.Log("Close button pressed");
            
            // Save config before closing
            _configService?.SaveConfigAsync();
            
            // Hide the popup
            await _uiService?.HidePopupAsync<OptionsPopup>();
        }
    }
}
