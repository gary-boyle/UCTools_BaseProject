using System;
using System.Linq;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using GrameFramework.Config;
using UCTools_ConfigVariables;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Popups
{
    /// <summary>
    /// Options popup that uses ConfigService for ScriptableObject queries
    /// 
    /// Intent: Clean UI controller that queries ConfigService for ScriptableObjects instead of direct references
    /// Design: Service-oriented approach with dependency injection via ConfigService
    /// Pros: 
    /// - No direct ScriptableObject dependencies
    /// - Easy testing with service mocking
    /// - Centralized ScriptableObject management
    /// - Clean separation of concerns
    /// Cons:
    /// - Requires ConfigService to be properly initialized
    /// - Runtime dependency resolution
    /// </summary>
    public class OptionsPopup : UIPopup
    {
        #region UI Elements
        // Audio
        private Toggle _toggleAudio;
        private SliderInt _masterVolumeSlider;
        private SliderInt _musicVolumeSlider;
        private SliderInt _sfxVolumeSlider;
        
        // Graphics
        private Toggle _toggleFullscreen;
        private Toggle _toggleVSync;
        private DropdownField _qualityDropdown;
        private DropdownField _resolutionDropdown;
        
        // Gameplay
        private DropdownField _difficultyDropdown;
        private Toggle _toggleAutoSave;
        private SliderInt _autoSaveIntervalSlider;
        
        // Input
        private Slider _mouseSensitivitySlider;
        private Toggle _toggleInvertYAxis;
        
        // Debug
        private Toggle _toggleShowFPS;
        private Toggle _toggleVerboseLogging;
        private Toggle _toggleConsoleEnabled;
        
        // Controls
        private Button _resetDefaultsButton;
        private Button _applyButton;
        private Button _closeButton;
        #endregion

        #region Services
        private readonly IConfigService _configService;
        private readonly IUIService _uiService;
        
        // Cached ScriptableObject references from ConfigService
        private AudioSettings_SO _audioSettings;
        private GraphicsSettings_SO _graphicsSettings;
        private GameplaySettings_SO _gameplaySettings;
        private InputSettings_SO _inputSettings;
        private DebugSettings_SO _debugSettings;
        #endregion
        
        public OptionsPopup(VisualElement rootElement) : base(rootElement)
        {
            _configService = GameManager.GetService<IConfigService>() ?? throw new ArgumentNullException(nameof(_configService));
            _uiService = GameManager.GetService<IUIService>() ?? throw new ArgumentNullException(nameof(_uiService));
            
            LoadScriptableObjectsFromConfigService();
            InitializeControls();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }

        /// <summary>
        /// Load ScriptableObject references from ConfigService
        /// </summary>
        private void LoadScriptableObjectsFromConfigService()
        {
            _audioSettings = _configService.GetConfigCategory<AudioSettings_SO>();
            _graphicsSettings = _configService.GetConfigCategory<GraphicsSettings_SO>();
            _gameplaySettings = _configService.GetConfigCategory<GameplaySettings_SO>();
            _inputSettings = _configService.GetConfigCategory<InputSettings_SO>();
            _debugSettings = _configService.GetConfigCategory<DebugSettings_SO>();
            
            // Validate that all required ScriptableObjects are available
            ValidateScriptableObjects();
        }

        /// <summary>
        /// Validate that all required ScriptableObjects were loaded successfully
        /// </summary>
        private void ValidateScriptableObjects()
        {
            if (_audioSettings == null)
                Debug.LogError("[OptionsPopup] AudioSettings_SO not found in ConfigService");
            if (_graphicsSettings == null)
                Debug.LogError("[OptionsPopup] GraphicsSettings_SO not found in ConfigService");
            if (_gameplaySettings == null)
                Debug.LogError("[OptionsPopup] GameplaySettings_SO not found in ConfigService");
            if (_inputSettings == null)
                Debug.LogError("[OptionsPopup] InputSettings_SO not found in ConfigService");
            if (_debugSettings == null)
                Debug.LogError("[OptionsPopup] DebugSettings_SO not found in ConfigService");
        }
        
        /// <summary>
        /// Initialize all UI controls and register callbacks
        /// </summary>
        private void InitializeControls()
        {
            CacheUIElements();
            SetupDropdowns();
            RegisterCallbacks();
        }

        /// <summary>
        /// Cache all UI element references
        /// </summary>
        private void CacheUIElements()
        {
            // Audio
            _toggleAudio = RootElement?.Q<Toggle>("tgl_EnableAudio");
            _masterVolumeSlider = RootElement?.Q<SliderInt>("slider_MasterVolume");
            _musicVolumeSlider = RootElement?.Q<SliderInt>("slider_MusicVolume");
            _sfxVolumeSlider = RootElement?.Q<SliderInt>("slider_SFXVolume");
            
            // Graphics
            _toggleFullscreen = RootElement?.Q<Toggle>("tgl_FullScreen");
            _toggleVSync = RootElement?.Q<Toggle>("tgl_Vsync");
            _qualityDropdown = RootElement?.Q<DropdownField>("dd_Quality");
            _resolutionDropdown = RootElement?.Q<DropdownField>("dd_Resolution");
            
            // Gameplay
            _difficultyDropdown = RootElement?.Q<DropdownField>("dd_Difficulty");
            _toggleAutoSave = RootElement?.Q<Toggle>("tgl_AutoSave");
            _autoSaveIntervalSlider = RootElement?.Q<SliderInt>("slider_AutoSaveInterval");
            
            // Input
            _mouseSensitivitySlider = RootElement?.Q<Slider>("slider_MouseSensitivity");
            _toggleInvertYAxis = RootElement?.Q<Toggle>("tgl_InvertYAxis");
            
            // Debug
            _toggleShowFPS = RootElement?.Q<Toggle>("tgl_ShowFPS");
            _toggleVerboseLogging = RootElement?.Q<Toggle>("tgl_VerboseLogging");
            _toggleConsoleEnabled = RootElement?.Q<Toggle>("tgl_ConsoleEnabled");
            
            // Controls
            _resetDefaultsButton = RootElement?.Q<Button>("btn_Reset");
            _applyButton = RootElement?.Q<Button>("btn_Apply");
            _closeButton = RootElement?.Q<Button>("btn_Close");
        }

        /// <summary>
        /// Setup dropdown choices from ScriptableObjects
        /// </summary>
        private void SetupDropdowns()
        {
            if (_qualityDropdown != null && _graphicsSettings != null)
            {
                _qualityDropdown.choices = _graphicsSettings.GetQualityChoices().ToList();
            }
            
            if (_resolutionDropdown != null && _graphicsSettings != null)
            {
                _resolutionDropdown.choices = _graphicsSettings.GetResolutionChoices().ToList();
            }
            
            if (_difficultyDropdown != null && _gameplaySettings != null)
            {
                _difficultyDropdown.choices = _gameplaySettings.GetDifficultyChoices().ToList();
            }
        }

        /// <summary>
        /// Register all UI event callbacks - delegates to ScriptableObjects
        /// </summary>
        private void RegisterCallbacks()
        {
            // Audio callbacks
            _toggleAudio?.RegisterCallback<ChangeEvent<bool>>(evt => _audioSettings?.SetAudioEnabled(evt.newValue));
            _masterVolumeSlider?.RegisterCallback<ChangeEvent<int>>(evt => _audioSettings?.SetMasterVolume(evt.newValue));
            _musicVolumeSlider?.RegisterCallback<ChangeEvent<int>>(evt => _audioSettings?.SetMusicVolume(evt.newValue));
            _sfxVolumeSlider?.RegisterCallback<ChangeEvent<int>>(evt => _audioSettings?.SetSfxVolume(evt.newValue));
            
            // Graphics callbacks
            _toggleFullscreen?.RegisterCallback<ChangeEvent<bool>>(evt => _graphicsSettings?.SetFullscreen(evt.newValue));
            _toggleVSync?.RegisterCallback<ChangeEvent<bool>>(evt => _graphicsSettings?.SetVSync(evt.newValue));
            _qualityDropdown?.RegisterValueChangedCallback(evt => {
                var choices = _graphicsSettings?.GetQualityChoices();
                var index = Array.IndexOf(choices, evt.newValue);
                if (index >= 0 && Enum.IsDefined(typeof(QualityOption), index))
                    _graphicsSettings?.SetQuality((QualityOption)index);
            });
            _resolutionDropdown?.RegisterValueChangedCallback(evt => {
                var choices = _graphicsSettings?.GetResolutionChoices();
                var index = Array.IndexOf(choices, evt.newValue);
                if (index >= 0 && Enum.IsDefined(typeof(ResolutionOption), index))
                    _graphicsSettings?.SetResolution((ResolutionOption)index);
            });
            
            // Gameplay callbacks
            _difficultyDropdown?.RegisterValueChangedCallback(evt => {
                var choices = _gameplaySettings?.GetDifficultyChoices();
                var index = Array.IndexOf(choices, evt.newValue);
                if (index >= 0) _gameplaySettings?.SetDifficulty(index);
            });
            _toggleAutoSave?.RegisterCallback<ChangeEvent<bool>>(evt => _gameplaySettings?.SetAutoSave(evt.newValue));
            _autoSaveIntervalSlider?.RegisterCallback<ChangeEvent<int>>(evt => _gameplaySettings?.SetAutoSaveInterval(evt.newValue));
            
            // Input callbacks
            _mouseSensitivitySlider?.RegisterCallback<ChangeEvent<float>>(evt => _inputSettings?.SetMouseSensitivity(evt.newValue));
            _toggleInvertYAxis?.RegisterCallback<ChangeEvent<bool>>(evt => _inputSettings?.SetInvertYAxis(evt.newValue));
            
            // Debug callbacks
            _toggleShowFPS?.RegisterCallback<ChangeEvent<bool>>(evt => _debugSettings?.SetShowFPS(evt.newValue));
            _toggleVerboseLogging?.RegisterCallback<ChangeEvent<bool>>(evt => _debugSettings?.SetVerboseLogging(evt.newValue));
            _toggleConsoleEnabled?.RegisterCallback<ChangeEvent<bool>>(evt => _debugSettings?.SetConsoleEnabled(evt.newValue));

            // Control callbacks
            _resetDefaultsButton?.RegisterCallback<ClickEvent>(OnResetDefaultsClicked);
            _applyButton?.RegisterCallback<ClickEvent>(OnApplyClicked);
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseClicked);
        }

        protected override void OnShow()
        {
            RefreshUI();
        }
        
        /// <summary>
        /// Refresh UI with current values from config service
        /// </summary>
        private void RefreshUI()
        {
            // Audio
            if (_toggleAudio != null) _toggleAudio.value = _configService.GetConfigValue<bool>("audio.enabled");
            if (_masterVolumeSlider != null) _masterVolumeSlider.value = _configService.GetConfigValue<int>("audio.master_volume");
            if (_musicVolumeSlider != null) _musicVolumeSlider.value = _configService.GetConfigValue<int>("audio.music_volume");
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = _configService.GetConfigValue<int>("audio.sfx_volume");
            
            // Graphics
            if (_toggleFullscreen != null) _toggleFullscreen.value = _configService.GetConfigValue<bool>("graphics.fullscreen");
            if (_toggleVSync != null) _toggleVSync.value = _configService.GetConfigValue<bool>("graphics.vsync");
            if (_qualityDropdown != null) _qualityDropdown.index = (int)_configService.GetConfigValue<QualityOption>("graphics.quality");
            if (_resolutionDropdown != null) _resolutionDropdown.index = (int)_configService.GetConfigValue<ResolutionOption>("graphics.resolution");
            
            // Gameplay
            if (_difficultyDropdown != null) _difficultyDropdown.index = _configService.GetConfigValue<int>("game.difficulty");
            if (_toggleAutoSave != null) _toggleAutoSave.value = _configService.GetConfigValue<bool>("game.auto_save");
            if (_autoSaveIntervalSlider != null) _autoSaveIntervalSlider.value = _configService.GetConfigValue<int>("game.auto_save_interval");
            
            // Input
            if (_mouseSensitivitySlider != null) _mouseSensitivitySlider.value = _configService.GetConfigValue<float>("input.mouse_sensitivity");
            if (_toggleInvertYAxis != null) _toggleInvertYAxis.value = _configService.GetConfigValue<bool>("input.invert_y_axis");
            
            // Debug
            if (_toggleShowFPS != null) _toggleShowFPS.value = _configService.GetConfigValue<bool>("debug.show_fps");
            if (_toggleVerboseLogging != null) _toggleVerboseLogging.value = _configService.GetConfigValue<bool>("debug.verbose_logging");
            if (_toggleConsoleEnabled != null) _toggleConsoleEnabled.value = _configService.GetConfigValue<bool>("debug.console_enabled");
        }

        #region Control Event Handlers
        private void OnResetDefaultsClicked(ClickEvent evt)
        {
            _configService?.ResetToDefaults();
            RefreshUI();
        }
        
        private async void OnApplyClicked(ClickEvent evt)
        {
            await _configService?.SaveConfigAsync();
        }
        
        private async void OnCloseClicked(ClickEvent evt)
        {
            await _configService?.SaveConfigAsync();
            await _uiService?.HidePopupAsync<OptionsPopup>();
        }
        #endregion
    }
}
