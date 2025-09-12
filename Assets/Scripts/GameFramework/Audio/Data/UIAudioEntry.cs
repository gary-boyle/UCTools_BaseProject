using System;
using UnityEngine;

namespace GameFramework.Audio.Data
{
    /// <summary>
    /// UI-specific audio entry that maps to UI interaction types
    /// </summary>
    [Serializable]
    public class UIAudioEntry
    {
        [SerializeField] private UIAudioType _audioType;
        [SerializeField] private AudioClip _clip;
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        public UIAudioType AudioType => _audioType;
        public AudioClip Clip => _clip;
        public float Volume => _volume;
        
        
    }
}