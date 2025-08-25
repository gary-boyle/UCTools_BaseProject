using System;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
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
        private Toggle _toggleAudio;
        private SliderInt _masterVolumeSlider;
        private SliderInt _musicVolumeSlider;
        private SliderInt _sfxVolumeSlider;
        private Button _resetDefaultsButton;
        private Button _closeButton;
        
        private readonly IConfigService _configService;
        private readonly IUIService _uiService;

        public OptionsPopup(VisualElement rootElement) : base(rootElement)
        {
            InitializeControls();
            
            _configService = GameManager.GetService<IConfigService>() ?? throw new ArgumentNullException(nameof(_configService));
            _uiService = GameManager.GetService<IUIService>() ?? throw new ArgumentNullException(nameof(_uiService));
            
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        private void InitializeControls()
        {
            _toggleAudio = RootElement?.Q<Toggle>("tgl_Sound");
            _masterVolumeSlider = RootElement?.Q<SliderInt>("slider_MasterVolume");
            _musicVolumeSlider = RootElement?.Q<SliderInt>("slider_MusicVolume");
            _sfxVolumeSlider = RootElement?.Q<SliderInt>("slider_SFXVolume");
            _resetDefaultsButton = RootElement?.Q<Button>("btn_Reset");
            _closeButton = RootElement?.Q<Button>("btn_Close");
            

            // Register callbacks
            _toggleAudio?.RegisterCallback<ChangeEvent<bool>>(OnToggleAudioChanged);
            _masterVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnMasterVolumeChanged);
            _musicVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnMusicVolumeChanged);
            _sfxVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnSfxVolumeChanged);
            _resetDefaultsButton?.RegisterCallback<ClickEvent>(OnResetDefaultsClicked);
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseClicked);
        }



        protected override void OnShow()
        {
            // Load current config values
            if (_configService != null)
            {
                if (_masterVolumeSlider != null)
                    _masterVolumeSlider.value = Mathf.RoundToInt(_configService.GetConfigValue<float>("audio.master_volume") * 100);
                if (_musicVolumeSlider != null)
                    _musicVolumeSlider.value = Mathf.RoundToInt(_configService.GetConfigValue<float>("audio.music_volume") * 100);
                if (_sfxVolumeSlider != null)
                    _sfxVolumeSlider.value = Mathf.RoundToInt(_configService.GetConfigValue<float>("audio.sfx_volume") * 100);
            }
        }
        
        private void OnToggleAudioChanged(ChangeEvent<bool> evt)
        {
            _configService?.SetConfigValue("audio.enabled", evt.newValue);
        }
        
        private void OnMasterVolumeChanged(ChangeEvent<int> evt)
        {
            _configService?.SetConfigValue("audio.master_volume", evt.newValue / 100f);
        }
        
        private void OnMusicVolumeChanged(ChangeEvent<int> evt)
        {
            _configService?.SetConfigValue("audio.music_volume", evt.newValue / 100f);
        }
        
        private void OnSfxVolumeChanged(ChangeEvent<int> evt)
        {
            _configService?.SetConfigValue("audio.sfx_volume", evt.newValue / 100f);
        }
        
        private void OnResetDefaultsClicked(ClickEvent evt)
        {
            _configService?.ResetToDefaults();
            
            // Refresh UI
            OnShow();
        }
        
        private async void OnCloseClicked(ClickEvent evt)
        {
            Debug.Log("have pressed the close button");
            // Save config before closing
            _configService?.SaveConfigAsync();
            
            // Hide the popup
            await _uiService?.HidePopupAsync<OptionsPopup>();
        }
    }
}
