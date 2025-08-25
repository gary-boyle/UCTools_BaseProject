using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine;
using GameFramework.StateMachine.Enum;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Options screen implementation with ConfigVar integration
    /// </summary>
    public class OptionsScreen : UIScreen
    {
        private Toggle _enableAudio;
        private SliderInt _masterVolumeSlider;
        private SliderInt _musicVolumeSlider;
        private SliderInt _sfxVolumeSlider;
        private Button _resetDefaultsButton;
        private Button _backButton;

        private readonly IConfigService _configService;
        
        public OptionsScreen(VisualElement rootElement) : base(rootElement)
        {
            InitializeControls();
            _configService = GameManager.GetService<IConfigService>();

        }
        
        private void InitializeControls()
        {
            _enableAudio = RootElement?.Q<Toggle>("tgl_EnableAudio");
            _masterVolumeSlider = RootElement?.Q<SliderInt>("MasterVolumeSlider");
            _musicVolumeSlider = RootElement?.Q<SliderInt>("MusicVolumeSlider");
            _sfxVolumeSlider = RootElement?.Q<SliderInt>("SfxVolumeSlider");
            _resetDefaultsButton = RootElement?.Q<Button>("ResetDefaultsButton");
            _backButton = RootElement?.Q<Button>("BackButton");
            
            // Register callbacks
            _enableAudio?.RegisterCallback<ChangeEvent<bool>>(OnAudioEnabledChanged);
            _masterVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnMasterVolumeChanged);
            _musicVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnMusicVolumeChanged);
            _sfxVolumeSlider?.RegisterCallback<ChangeEvent<int>>(OnSfxVolumeChanged);
            _resetDefaultsButton?.RegisterCallback<ClickEvent>(OnResetDefaultsClicked);
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
        }


        protected override void OnShow()
        {
            // // Load current config values
            // if (_configService != null)
            // {
            //     if (_enableAudio != null)
            //         if (_configService.GetConfigValue<float>("audio.enabled") == 1f)
            //         {
            //             _enableAudio.value = 1f;
            //         }
            //         else
            //         {
            //             _enableAudio.value = 0f;
            //         }
            //     if (_masterVolumeSlider != null)
            //         _masterVolumeSlider.value = Mathf.RoundToInt(_configService.GetConfigValue<float>("audio.master_volume") * 100);
            //     if (_musicVolumeSlider != null)
            //         _musicVolumeSlider.value = Mathf.RoundToInt(_configService.GetConfigValue<float>("audio.music_volume") * 100);
            //     if (_sfxVolumeSlider != null)
            //         _sfxVolumeSlider.value = Mathf.RoundToInt(_configService.GetConfigValue<float>("audio.sfx_volume") * 100);
            // }
        }
        
        
        private void OnAudioEnabledChanged(ChangeEvent<bool> evt)
        {
            _configService?.SetConfigValue("audio.enabled", evt.newValue);
        }
        
        private void OnMasterVolumeChanged(ChangeEvent<int> evt)
        {
            _configService?.SetConfigValue("audio.master_volume", evt.newValue);
        }
        
        private void OnMusicVolumeChanged(ChangeEvent<int> evt)
        {
            _configService?.SetConfigValue("audio.music_volume", evt.newValue);
        }
        
        private void OnSfxVolumeChanged(ChangeEvent<int> evt)
        {
            _configService?.SetConfigValue("audio.sfx_volume", evt.newValue);
        }
        
        private void OnResetDefaultsClicked(ClickEvent evt)
        {
            _configService?.ResetToDefaults();
            
            // Refresh UI
            OnShow();
        }
        
        private void OnBackClicked(ClickEvent evt)
        {
            // Save config and go back
            _configService?.SaveConfigAsync();
            
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