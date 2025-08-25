using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine;
using GameFramework.StateMachine.Enum;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Options screen implementation with ConfigVar integration
    /// </summary>
    public class OptionsScreen : UIScreen
    {
        private SliderInt _masterVolumeSlider;
        private SliderInt _musicVolumeSlider;
        private SliderInt _sfxVolumeSlider;
        private Button _resetDefaultsButton;
        private Button _backButton;
        
        public OptionsScreen(VisualElement rootElement) : base(rootElement)
        {
            InitializeControls();
        }
        
        private void InitializeControls()
        {
            _masterVolumeSlider = RootElement?.Q<SliderInt>("MasterVolumeSlider");
            _musicVolumeSlider = RootElement?.Q<SliderInt>("MusicVolumeSlider");
            _sfxVolumeSlider = RootElement?.Q<SliderInt>("SfxVolumeSlider");
            _resetDefaultsButton = RootElement?.Q<Button>("ResetDefaultsButton");
            _backButton = RootElement?.Q<Button>("BackButton");
            
            // Register callbacks
            _masterVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnMasterVolumeChanged);
            _musicVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnMusicVolumeChanged);
            _sfxVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnSfxVolumeChanged);
            _resetDefaultsButton?.RegisterCallback<ClickEvent>(OnResetDefaultsClicked);
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
        }
        
        protected override void OnShow()
        {
            // Load current config values
            var configService = GameManager.GetService<IConfigService>();
            if (configService != null)
            {
                if (_masterVolumeSlider != null)
                    _masterVolumeSlider.value = Mathf.RoundToInt(configService.GetConfigValue<float>("audio.master_volume") * 100);
                if (_musicVolumeSlider != null)
                    _musicVolumeSlider.value = Mathf.RoundToInt(configService.GetConfigValue<float>("audio.music_volume") * 100);
                if (_sfxVolumeSlider != null)
                    _sfxVolumeSlider.value = Mathf.RoundToInt(configService.GetConfigValue<float>("audio.sfx_volume") * 100);
            }
        }
        
        private void OnMasterVolumeChanged(ChangeEvent<int> evt)
        {
            var configService = GameManager.GetService<IConfigService>();
            configService?.SetConfigValue("audio.master_volume", evt.newValue / 100f);
        }
        
        private void OnMusicVolumeChanged(ChangeEvent<int> evt)
        {
            var configService = GameManager.GetService<IConfigService>();
            configService?.SetConfigValue("audio.music_volume", evt.newValue / 100f);
        }
        
        private void OnSfxVolumeChanged(ChangeEvent<int> evt)
        {
            var configService = GameManager.GetService<IConfigService>();
            configService?.SetConfigValue("audio.sfx_volume", evt.newValue / 100f);
        }
        
        private void OnResetDefaultsClicked(ClickEvent evt)
        {
            var configService = GameManager.GetService<IConfigService>();
            configService?.ResetToDefaults();
            
            // Refresh UI
            OnShow();
        }
        
        private void OnBackClicked(ClickEvent evt)
        {
            // Save config and go back
            var configService = GameManager.GetService<IConfigService>();
            configService?.SaveConfigAsync();
            
            // Determine where to go back to based on current state
            var stateMachine = GameManager.GetService<IGameStateMachine>();
            if (stateMachine?.CurrentStateType == GameStateType.Options)
            {
                // Go back to previous state (would need history tracking for this)
                var eventSystem = GameManager.GetService<IEventSystem>();
                eventSystem?.Publish(new MainMenuRequestedEvent()); // Default to main menu
            }
        }
    }
}