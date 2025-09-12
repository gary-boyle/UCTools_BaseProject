using System;
using System.Threading.Tasks;
using GameFramework.Audio;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;
using UnityEngine.Audio;
using IAudioService = GameFramework.Services.Interfaces.IAudioService;
using IConfigService = GameFramework.Services.Interfaces.IConfigService;

namespace GameFramework.Services
{
    /// <summary>
    /// AudioService with intelligent music management
    /// Compares requested clips against currently playing clips to avoid unnecessary restarts
    /// 
    /// INTENT: Centralize all audio intelligence in the service layer
    /// DESIGN: Compare actual AudioClips rather than relying on external state checks
    /// PROS: Clean separation of concerns, states stay simple, centralized audio logic
    /// CONS: Requires clip comparison overhead (minimal)
    /// </summary>
    public class AudioService : IAudioService, IUpdatable
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly IConfigService _configService;
        
        // Audio components
        private AudioSource _musicSource;
        private AudioSource _uiAudioSource;
        private AudioMixer _masterMixer;
        
        // Asset management
        private AudioDatabase_SO _audioDatabase;
        private AudioManager _audioManager;
        
        // Mixer parameter names
        private const string MASTER_VOLUME_PARAM = "MasterVolume";
        private const string MUSIC_VOLUME_PARAM = "MusicVolume";
        private const string SFX_VOLUME_PARAM = "SFXVolume";
        private const string UI_VOLUME_PARAM = "UIVolume";
        
        // Volume conversion constants
        private const float MIN_MIXER_VOLUME = -80f;
        private const float MAX_MIXER_VOLUME = 0f;
        
        // Fade system state
        private bool _isFading = false;
        private FadeType _currentFadeType = FadeType.None;
        private float _fadeStartTime;
        private float _fadeDuration;
        private float _fadeStartVolume;
        private float _fadeTargetVolume;

        private enum FadeType
        {
            None,
            FadeIn,
            FadeOut
        }

        public AudioService(IEventSystem eventSystem, IConfigService configService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }
        
        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }
        
        public async Task InitializeAsync(AudioManager audioManager)
        {
            if (IsInitialized) return;

            _audioManager = audioManager;
            
            // Get audio components
            _musicSource = audioManager.MusicSource;
            _uiAudioSource = audioManager.UISource;
            _masterMixer = audioManager.MasterMixer;
            
            // Validate mixer setup
            ValidateMixerSetup();
            
            // Load audio assets
            await LoadAudioAssets();
            
            // Subscribe to events
            SubscribeToAudioEvents();
            _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);
            
            // Apply initial audio settings
            ApplyAudioSettings();
            
            IsInitialized = true;
        }

        /// <summary>
        /// Check if the requested clip is the same as currently playing
        /// </summary>
        private bool IsSameClipPlaying(AudioClip requestedClip)
        {
            return _musicSource != null && 
                   _musicSource.isPlaying && 
                   _musicSource.clip == requestedClip;
        }

        /// <summary>
        /// Check if any music is currently playing
        /// </summary>
        private bool IsMusicCurrentlyPlaying()
        {
            return _musicSource != null && _musicSource.isPlaying;
        }

        /// <summary>
        /// Validate that the AudioMixer is properly configured
        /// </summary>
        private void ValidateMixerSetup()
        {
            if (_masterMixer == null)
            {
                throw new InvalidOperationException("[AudioService] Master AudioMixer is not assigned");
            }

            if (!TrySetMixerParameter(MASTER_VOLUME_PARAM, 0f))
            {
                Debug.LogWarning($"[AudioService] AudioMixer parameter '{MASTER_VOLUME_PARAM}' not found. Make sure it's exposed in the mixer.");
            }
        }

        /// <summary>
        /// Update method handles music fade operations
        /// </summary>
        public void Update()
        {
            if (_isFading)
            {
                UpdateFadeOperation();
            }
        }

        /// <summary>
        /// Handle fade operations using mixer volume control
        /// </summary>
        private void UpdateFadeOperation()
        {
            float elapsedTime = Time.time - _fadeStartTime;
            float progress = Mathf.Clamp01(elapsedTime / _fadeDuration);

            switch (_currentFadeType)
            {
                case FadeType.FadeIn:
                    UpdateFadeIn(progress);
                    break;
                    
                case FadeType.FadeOut:
                    UpdateFadeOut(progress);
                    break;
            }

            if (progress >= 1f)
            {
                CompleteFadeOperation();
            }
        }

        private void UpdateFadeIn(float progress)
        {
            float currentVolume = Mathf.Lerp(_fadeStartVolume, _fadeTargetVolume, progress);
            float mixerVolume = ConvertToMixerVolume(currentVolume);
            _masterMixer.SetFloat(MUSIC_VOLUME_PARAM, mixerVolume);
        }

        private void UpdateFadeOut(float progress)
        {
            float currentVolume = Mathf.Lerp(_fadeStartVolume, _fadeTargetVolume, progress);
            float mixerVolume = ConvertToMixerVolume(currentVolume);
            _masterMixer.SetFloat(MUSIC_VOLUME_PARAM, mixerVolume);
        }
        
        private void ResetFadeState()
        {
            _isFading = false;
            _currentFadeType = FadeType.None;
            _fadeStartTime = 0f;
            _fadeDuration = 0f;
            _fadeStartVolume = 0f;
            _fadeTargetVolume = 0f;
        }

        public void Shutdown()
        {
            UnsubscribeFromAudioEvents();
            _eventSystem.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
            
            ResetFadeState();
            
            if (_audioManager != null)
                UnityEngine.Object.DestroyImmediate(_audioManager);
                
            IsInitialized = false;
        }

        private async Task LoadAudioAssets()
        {
            _audioDatabase = _audioManager.AudioDatabaseSO;
            
            if (_audioDatabase == null)
            {
                throw new InvalidOperationException("[AudioService] AudioDatabase not found");
            }
            
            _audioDatabase.Initialize();
            await Task.CompletedTask;
        }

        private void SubscribeToAudioEvents()
        {
            _eventSystem.Subscribe<AudioEvents.PlayMusicEvent>(OnPlayMusicRequested);
            _eventSystem.Subscribe<AudioEvents.StopMusicEvent>(OnStopMusicRequested);
            _eventSystem.Subscribe<AudioEvents.UIAudioEvent>(OnUIAudioRequested);
        }

        private void UnsubscribeFromAudioEvents()
        {
            _eventSystem.Unsubscribe<AudioEvents.PlayMusicEvent>(OnPlayMusicRequested);
            _eventSystem.Unsubscribe<AudioEvents.StopMusicEvent>(OnStopMusicRequested);
            _eventSystem.Unsubscribe<AudioEvents.UIAudioEvent>(OnUIAudioRequested);
        }

        #region Event Handlers

        /// <summary>
        /// Handle music play requests with intelligent clip comparison
        /// Only starts new music if the requested clip is different from what's currently playing
        /// </summary>
        private void OnPlayMusicRequested(AudioEvents.PlayMusicEvent evt)
        {
            var requestedClip = _audioDatabase?.GetMusicClip(evt.MusicId);
            if (requestedClip == null) return;

            // Check if the same clip is already playing
            if (IsSameClipPlaying(requestedClip))
            {
                // Same music is already playing, do nothing
                return;
            }

            // Different clip or no music playing - proceed with the request
            if (evt.FadeIn)
            {
                // If different music is playing, we might want to fade out first
                if (IsMusicCurrentlyPlaying())
                {
                    // Cross-fade: quickly fade out current, then fade in new
                    PlayMusicWithCrossFade(requestedClip, evt.FadeTime, evt.Loop);
                }
                else
                {
                    // No music playing, just fade in
                    PlayMusicWithFade(requestedClip, evt.FadeTime, evt.Loop);
                }
            }
            else
            {
                PlayMusicImmediate(requestedClip, evt.Loop);
            }
        }

        private void OnStopMusicRequested(AudioEvents.StopMusicEvent evt)
        {
            if (!IsMusicCurrentlyPlaying()) return;
            
            if (evt.FadeOut)
            {
                StopMusicWithFade(evt.FadeTime);
            }
            else
            {
                StopMusicImmediate();
            }
        }

        private void OnUIAudioRequested(AudioEvents.UIAudioEvent evt)
        {
            AudioClip clip = null;

            if (!string.IsNullOrEmpty(evt.CustomSoundId))
            {
                clip = _audioDatabase?.GetSFXClip(evt.CustomSoundId);
            }
            else
            {
                clip = _audioDatabase?.GetUIAudioClip(evt.AudioType);
            }

            if (clip != null)
            {
                _uiAudioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Music Playback Methods

        /// <summary>
        /// Play music immediately without fading
        /// </summary>
        private void PlayMusicImmediate(AudioClip clip, bool loop)
        {
            ResetFadeState();
            
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        /// <summary>
        /// Play music with fade in effect
        /// </summary>
        private void PlayMusicWithFade(AudioClip clip, float fadeTime, bool loop)
        {
            ResetFadeState();
            
            // Start music at zero volume
            SetMixerVolume(MUSIC_VOLUME_PARAM, 0f);
            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.Play();
            
            // Setup fade
            _isFading = true;
            _currentFadeType = FadeType.FadeIn;
            _fadeStartTime = Time.time;
            _fadeDuration = fadeTime;
            _fadeStartVolume = 0f;
            _fadeTargetVolume = GetMusicVolume();
        }

        /// <summary>
        /// Cross-fade from current music to new music
        /// </summary>
        private void PlayMusicWithCrossFade(AudioClip newClip, float fadeTime, bool loop)
        {
            // For simplicity, just do a quick fade out then fade in
            // You could implement true cross-fading with two AudioSources if needed
            
            if (_isFading)
            {
                // Already fading, just switch immediately
                PlayMusicImmediate(newClip, loop);
                return;
            }

            // Quick fade out current music (quarter of the time), then start new music
            var quickFadeTime = fadeTime * 0.25f;
            
            ResetFadeState();
            _isFading = true;
            _currentFadeType = FadeType.FadeOut;
            _fadeStartTime = Time.time;
            _fadeDuration = quickFadeTime;
            _fadeStartVolume = GetMusicVolume();
            _fadeTargetVolume = 0f;
            
            // Store the new clip info for after fade out completes
            _pendingMusicClip = newClip;
            _pendingMusicLoop = loop;
            _pendingFadeInTime = fadeTime - quickFadeTime;
        }
        
        // Fields for cross-fade functionality
        private AudioClip _pendingMusicClip;
        private bool _pendingMusicLoop;
        private float _pendingFadeInTime;

        /// <summary>
        /// Enhanced fade completion that handles cross-fading
        /// </summary>
        private void CompleteFadeOperation()
        {
            switch (_currentFadeType)
            {
                case FadeType.FadeIn:
                    SetMixerVolume(MUSIC_VOLUME_PARAM, _fadeTargetVolume);
                    break;
                    
                case FadeType.FadeOut:
                    SetMixerVolume(MUSIC_VOLUME_PARAM, 0f);
                    _musicSource.Stop();
                    
                    // Check if we have pending music to start (cross-fade)
                    if (_pendingMusicClip != null)
                    {
                        var pendingClip = _pendingMusicClip;
                        var pendingLoop = _pendingMusicLoop;
                        var pendingFadeTime = _pendingFadeInTime;
                        
                        // Clear pending state
                        _pendingMusicClip = null;
                        _pendingMusicLoop = false;
                        _pendingFadeInTime = 0f;
                        
                        // Start the new music with fade in
                        PlayMusicWithFade(pendingClip, pendingFadeTime, pendingLoop);
                        return; // Don't reset fade state, we're starting a new fade
                    }
                    break;
            }

            ResetFadeState();
        }

        /// <summary>
        /// Stop music immediately without fading
        /// </summary>
        private void StopMusicImmediate()
        {
            ResetFadeState();
            _musicSource.Stop();
            
            // Clear any pending cross-fade
            _pendingMusicClip = null;
            _pendingMusicLoop = false;
            _pendingFadeInTime = 0f;
        }

        /// <summary>
        /// Stop music with fade out effect
        /// </summary>
        private void StopMusicWithFade(float fadeTime)
        {
            ResetFadeState();
            
            _isFading = true;
            _currentFadeType = FadeType.FadeOut;
            _fadeStartTime = Time.time;
            _fadeDuration = fadeTime;
            _fadeStartVolume = GetMusicVolume();
            _fadeTargetVolume = 0f;
            
            // Clear any pending cross-fade
            _pendingMusicClip = null;
            _pendingMusicLoop = false;
            _pendingFadeInTime = 0f;
        }

        #endregion

        #region Mixer Volume Control

        private float ConvertToMixerVolume(float normalizedVolume)
        {
            if (normalizedVolume <= 0f)
                return MIN_MIXER_VOLUME;
            
            return Mathf.Clamp(20f * Mathf.Log10(normalizedVolume), MIN_MIXER_VOLUME, MAX_MIXER_VOLUME);
        }

        private float ConvertFromMixerVolume(float mixerVolume)
        {
            if (mixerVolume <= MIN_MIXER_VOLUME)
                return 0f;
                
            return Mathf.Pow(10f, mixerVolume / 20f);
        }

        private void SetMixerVolume(string parameterName, float normalizedVolume)
        {
            float mixerVolume = ConvertToMixerVolume(normalizedVolume);
            _masterMixer.SetFloat(parameterName, mixerVolume);
        }

        private float GetMixerVolume(string parameterName)
        {
            if (_masterMixer.GetFloat(parameterName, out float mixerVolume))
            {
                return ConvertFromMixerVolume(mixerVolume);
            }
            return 1f;
        }

        private bool TrySetMixerParameter(string parameterName, float value)
        {
            return _masterMixer.SetFloat(parameterName, value);
        }

        #endregion

        /// <summary>
        /// Provide access to mixer groups for GameObjects
        /// </summary>
        public AudioMixerGroup GetSFXMixerGroup() => _audioManager.SFXMixerGroup;

        /// <summary>
        /// Provide access to the audio database for individual GameObjects
        /// </summary>
        public AudioDatabase_SO GetAudioDatabase() => _audioDatabase;

        private void OnOptionsChanged(OptionsChangedEvent evt)
        {
            ApplyAudioSettings();
        }
        
        /// <summary>
        /// Apply current audio settings from config with proper audio enabled handling
        /// </summary>
        private void ApplyAudioSettings()
        {
            var audioEnabled = _configService.GetConfigValue<bool>("audio.enabled");
            var masterVolume = _configService.GetConfigValue<float>("audio.master_volume");
            var musicVolume = _configService.GetConfigValue<float>("audio.music_volume");
            var sfxVolume = _configService.GetConfigValue<float>("audio.sfx_volume");
            var uiVolume = _configService.GetConfigValue<float>("audio.ui_volume");
    
            // Apply audio enabled state first
            if (audioEnabled)
            {
                SetMasterVolume(masterVolume);
                SetMusicVolume(musicVolume);
                SetSFXVolume(sfxVolume);
                SetUIVolume(uiVolume);
            }
            else
            {
                // Mute all audio by setting master to minimum
                SetMasterVolume(0f);
            }
        }

        
        public void SetMasterVolume(float volume)
        {
            SetMixerVolume(MASTER_VOLUME_PARAM, volume);
        }
        
        public void SetMusicVolume(float volume)
        {
            if (!_isFading)
            {
                SetMixerVolume(MUSIC_VOLUME_PARAM, volume);
            }
            else if (_currentFadeType == FadeType.FadeIn)
            {
                _fadeTargetVolume = volume;
            }
        }
        
        public void SetSFXVolume(float volume)
        {
            SetMixerVolume(SFX_VOLUME_PARAM, volume);
        }

        public void SetUIVolume(float volume)
        {
            SetMixerVolume(UI_VOLUME_PARAM, volume);
        }
        
        public float GetMasterVolume()
        {
            var audioEnabled = _configService.GetConfigValue<bool>("audio.enabled");
            var masterVolume = _configService.GetConfigValue<float>("audio.master_volume");
            return audioEnabled ? masterVolume : 0f;
        }

        public float GetMusicVolume()
        {
            var audioEnabled = _configService.GetConfigValue<bool>("audio.enabled");
            var musicVolume = _configService.GetConfigValue<float>("audio.music_volume");
            return audioEnabled ? musicVolume : 0f;
        }

        public float GetSFXVolume()
        {
            var audioEnabled = _configService.GetConfigValue<bool>("audio.enabled");
            var sfxVolume = _configService.GetConfigValue<float>("audio.sfx_volume");
            return audioEnabled ? sfxVolume : 0f;
        }

        public float GetUIVolume()
        {
            var audioEnabled = _configService.GetConfigValue<bool>("audio.enabled");
            var uiVolume = _configService.GetConfigValue<float>("audio.ui_volume");
            return audioEnabled ? uiVolume : 0f;
        }
    }
}
