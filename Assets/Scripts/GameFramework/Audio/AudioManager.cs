using UnityEngine;
using UnityEngine.Audio;

namespace GameFramework.Audio
{
    /// <summary>
    /// MonoBehaviour that manages audio components and mixer references
    /// Provides the physical audio setup for the AudioService
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _uiSource;
        
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _masterMixer;
        [SerializeField] private AudioMixerGroup _musicMixerGroup;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;
        [SerializeField] private AudioMixerGroup _uiMixerGroup;
        
        [Header("Audio Database")]
        [SerializeField] private AudioDatabase_SO _audioDatabaseSO;

        // Properties for service access
        public AudioSource MusicSource => _musicSource;
        public AudioSource UISource => _uiSource;
        public AudioMixer MasterMixer => _masterMixer;
        public AudioMixerGroup MusicMixerGroup => _musicMixerGroup;
        public AudioMixerGroup SFXMixerGroup => _sfxMixerGroup;
        public AudioMixerGroup UIMixerGroup => _uiMixerGroup;
        public AudioDatabase_SO AudioDatabaseSO => _audioDatabaseSO;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ConfigureAudioSources();
        }

        /// <summary>
        /// Configure audio sources with appropriate mixer groups
        /// </summary>
        private void ConfigureAudioSources()
        {
            if (_musicSource != null && _musicMixerGroup != null)
            {
                _musicSource.outputAudioMixerGroup = _musicMixerGroup;
                _musicSource.playOnAwake = false;
                _musicSource.loop = true;
            }

            if (_uiSource != null && _uiMixerGroup != null)
            {
                _uiSource.outputAudioMixerGroup = _uiMixerGroup;
                _uiSource.playOnAwake = false;
                _uiSource.loop = false;
            }
        }

        /// <summary>
        /// Validate setup in editor
        /// </summary>
        private void OnValidate()
        {
            if (_masterMixer == null)
            {
                Debug.LogWarning("[AudioManager] Master AudioMixer is not assigned!");
            }

            if (_musicSource == null || _uiSource == null)
            {
                Debug.LogWarning("[AudioManager] Audio sources are not properly assigned!");
            }
        }
    }
}
