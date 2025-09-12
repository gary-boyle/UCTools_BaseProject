using GameFramework.Core;
using GameFramework.Services.Interfaces;
using UnityEngine;
using UnityEngine.Audio;

namespace GameFramework.Audio
{
    /// <summary>
    /// GameObject audio component that integrates with the master audio mixer
    /// Automatically uses the SFX mixer group for proper volume control
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GameObjectAudio : MonoBehaviour
    {
        [Header("Audio Configuration")]
        [SerializeField] private bool _use3DAudio = true;
        [SerializeField] private bool _playOnAwake = false;
        
        private AudioSource _audioSource;
        private AudioDatabase_SO _audioDatabase;
        private IAudioService _audioService;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            ConfigureAudioSource();
        }

        private void Start()
        {
            InitializeServices();
        }

        /// <summary>
        /// Configure the AudioSource component with mixer integration
        /// </summary>
        private void ConfigureAudioSource()
        {
            _audioSource.playOnAwake = _playOnAwake;
            _audioSource.spatialBlend = _use3DAudio ? 1f : 0f;
        }

        /// <summary>
        /// Initialize services and setup mixer group
        /// </summary>
        private void InitializeServices()
        {
            _audioService = GameManager.GetService<IAudioService>();
            _audioDatabase = _audioService?.GetAudioDatabase();
            
            // Automatically assign SFX mixer group
            var sfxMixerGroup = _audioService?.GetSFXMixerGroup();
            if (sfxMixerGroup != null)
            {
                _audioSource.outputAudioMixerGroup = sfxMixerGroup;
            }
        }

        /// <summary>
        /// Play a sound effect by ID
        /// Volume is controlled by the mixer, so we use full volume here
        /// </summary>
        public void PlaySound(string soundId, float volume = 1f, float pitch = 1f)
        {
            if (_audioDatabase == null) return;

            var clip = _audioDatabase.GetSFXClip(soundId);
            if (clip != null)
            {
                PlaySound(clip, volume, pitch);
            }
        }

        /// <summary>
        /// Play a sound effect with AudioClip directly
        /// The mixer handles global SFX volume control
        /// </summary>
        public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _audioSource == null) return;

            var originalPitch = _audioSource.pitch;
            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(clip, volume); // No need to multiply by SFX volume - mixer handles it
            _audioSource.pitch = originalPitch;
        }

        /// <summary>
        /// Play a looping sound by ID
        /// </summary>
        public void PlayLoopingSound(string soundId, float volume = 1f, float pitch = 1f)
        {
            if (_audioDatabase == null) return;

            var clip = _audioDatabase.GetSFXClip(soundId);
            if (clip != null)
            {
                PlayLoopingSound(clip, volume, pitch);
            }
        }

        /// <summary>
        /// Play a looping sound with AudioClip directly
        /// </summary>
        public void PlayLoopingSound(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _audioSource == null) return;

            _audioSource.clip = clip;
            _audioSource.volume = volume; // Mixer handles global volume
            _audioSource.pitch = pitch;
            _audioSource.loop = true;
            _audioSource.Play();
        }

        // Rest of the methods remain the same...
        public void StopSound() => _audioSource?.Stop();
        public void PauseSound() => _audioSource?.Pause();
        public void ResumeSound() => _audioSource?.UnPause();
        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;
        
        public void SetVolume(float volume)
        {
            if (_audioSource != null)
            {
                _audioSource.volume = volume; // Mixer handles global multiplier
            }
        }
        
        public void SetPitch(float pitch)
        {
            if (_audioSource != null)
            {
                _audioSource.pitch = pitch;
            }
        }
        
        public void Set3DAudio(bool enable3D)
        {
            _use3DAudio = enable3D;
            if (_audioSource != null)
            {
                _audioSource.spatialBlend = enable3D ? 1f : 0f;
            }
        }
    }
}
