
using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Audio.Data;

namespace GameFramework.Audio
{
    /// <summary>
    /// ScriptableObject database for managing audio assets
    /// Provides centralized audio clip management without MonoBehaviour dependencies
    /// </summary>
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "Game Framework/Audio/Audio Database")]
    public class AudioDatabase_SO : ScriptableObject
    {
        [Header("Music Tracks")]
        [SerializeField] private List<AudioEntry> _musicTracks;
        
        [Header("Sound Effects")]
        [SerializeField] private List<AudioEntry> _soundEffects;
        
        [Header("UI Sounds")]
        [SerializeField] private List<UIAudioEntry> _uiSounds;

        // Runtime dictionaries for fast lookup
        private Dictionary<string, AudioClip> _musicLookup;
        private Dictionary<string, AudioClip> _sfxLookup;
        private Dictionary<UIAudioType, AudioClip> _uiAudioLookup;

        /// <summary>
        /// Initialize lookup dictionaries for runtime performance
        /// </summary>
        public void Initialize()
        {
            InitializeMusicLookup();
            InitializeSFXLookup();
            InitializeUIAudioLookup();
        }

        void Reset()
        {
            _musicTracks = new List<AudioEntry>() { new AudioEntry() };
            _uiSounds = new List<UIAudioEntry>() { new UIAudioEntry() };
            _soundEffects = new List<AudioEntry>() { new AudioEntry() };
        }
        
        private void InitializeMusicLookup()
        {
            _musicLookup = new Dictionary<string, AudioClip>();
            foreach (var entry in _musicTracks)
            {
                if (!string.IsNullOrEmpty(entry.Id) && entry.Clip != null)
                {
                    _musicLookup[entry.Id] = entry.Clip;
                }
            }
        }

        private void InitializeSFXLookup()
        {
            _sfxLookup = new Dictionary<string, AudioClip>();
            foreach (var entry in _soundEffects)
            {
                if (!string.IsNullOrEmpty(entry.Id) && entry.Clip != null)
                {
                    _sfxLookup[entry.Id] = entry.Clip;
                }
            }
        }

        private void InitializeUIAudioLookup()
        {
            _uiAudioLookup = new Dictionary<UIAudioType, AudioClip>();
            foreach (var entry in _uiSounds)
            {
                if (entry.Clip != null)
                {
                    _uiAudioLookup[entry.AudioType] = entry.Clip;
                }
            }
        }

        public AudioClip GetMusicClip(string musicId)
        {
            return _musicLookup?.GetValueOrDefault(musicId);
        }

        public AudioClip GetSFXClip(string soundId)
        {
            return _sfxLookup?.GetValueOrDefault(soundId);
        }

        public AudioClip GetUIAudioClip(UIAudioType audioType)
        {
            return _uiAudioLookup?.GetValueOrDefault(audioType);
        }

        public IReadOnlyList<AudioEntry> MusicTracks => _musicTracks.AsReadOnly();
        public IReadOnlyList<AudioEntry> SoundEffects => _soundEffects.AsReadOnly();
        public IReadOnlyList<UIAudioEntry> UISounds => _uiSounds.AsReadOnly();
    }

}