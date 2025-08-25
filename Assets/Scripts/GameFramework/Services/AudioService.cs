using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;
using UnityEngine.Audio;
using IAudioService = GameFramework.Services.Interfaces.IAudioService;
using IConfigService = GameFramework.Services.Interfaces.IConfigService;

namespace GameFramework.Services
{
    /// <summary>
    /// Audio service implementation with constructor injection
    /// </summary>
    public class AudioService : IAudioService
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IEventSystem _eventSystem;
        private readonly IConfigService _configService;
        
        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioMixer _masterMixer;
        private Dictionary<string, AudioClip> _musicClips = new();
        private Dictionary<string, AudioClip> _sfxClips = new();
        
        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public AudioService(IEventSystem eventSystem, IConfigService configService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            Debug.Log("[AudioService] Initializing audio system...");
            
            // Create audio sources
            var audioObject = new GameObject("AudioManager");
            GameObject.DontDestroyOnLoad(audioObject);
            
            _musicSource = audioObject.AddComponent<AudioSource>();
            _sfxSource = audioObject.AddComponent<AudioSource>();
            
            _musicSource.loop = true;
            _sfxSource.loop = false;
            
            // Load audio clips (implement your audio loading logic)
            await LoadAudioClips();
            
            // Subscribe to config changes using injected event system
            _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);
            
            IsInitialized = true;
        }
        
        public void Shutdown()
        {
            _eventSystem.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
            IsInitialized = false;
        }
        
        private async Task LoadAudioClips()
        {
            // Implement your audio clip loading logic here
            await Task.CompletedTask;
        }
        
        private void OnOptionsChanged(OptionsChangedEvent evt)
        {
            // Update audio settings when options change
            ApplyAudioSettings();
        }
        
        private void ApplyAudioSettings()
        {
            // Use injected config service to get audio settings
            var masterVolume = _configService.GetConfigValue<float>("audio.master_volume");
            var musicVolume = _configService.GetConfigValue<float>("audio.music_volume");
            var sfxVolume = _configService.GetConfigValue<float>("audio.sfx_volume");
            
            SetMasterVolume(masterVolume);
            SetMusicVolume(musicVolume);
            SetSFXVolume(sfxVolume);
        }
        
        public void PlayMusic(string musicName)
        {
            if (_musicClips.TryGetValue(musicName, out var clip))
            {
                _musicSource.clip = clip;
                _musicSource.Play();
            }
            else
            {
                Debug.LogWarning($"[AudioService] Music clip '{musicName}' not found");
            }
        }
        
        public void PlaySound(string soundName)
        {
            if (_sfxClips.TryGetValue(soundName, out var clip))
            {
                _sfxSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning($"[AudioService] SFX clip '{soundName}' not found");
            }
        }
        
        public void StopMusic()
        {
            _musicSource.Stop();
        }
        
        public void StopSound(string soundName)
        {
            // For one-shot sounds, we can't easily stop specific sounds
            // You might want to maintain a list of playing sounds for this
            _sfxSource.Stop();
        }
        
        public void SetMasterVolume(float volume)
        {
            if (_masterMixer != null)
            {
                _masterMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
            }
        }
        
        public void SetMusicVolume(float volume)
        {
            _musicSource.volume = volume;
        }
        
        public void SetSFXVolume(float volume)
        {
            _sfxSource.volume = volume;
        }
        
        public float GetMasterVolume()
        {
            return _configService.GetConfigValue<float>("audio.master_volume");
        }
        
        public float GetMusicVolume()
        {
            return _configService.GetConfigValue<float>("audio.music_volume");
        }
        
        public float GetSFXVolume()
        {
            return _configService.GetConfigValue<float>("audio.sfx_volume");
        }
    }
}