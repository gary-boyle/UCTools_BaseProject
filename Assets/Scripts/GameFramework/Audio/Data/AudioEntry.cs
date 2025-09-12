using System;
using UnityEngine;

namespace GameFramework.Audio.Data
{
    /// <summary>
    /// Represents an audio clip with metadata
    /// </summary>
    [Serializable]
    public class AudioEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private string _description;
        [SerializeField, Range(0f, 1f)] private float _defaultVolume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float _defaultPitch = 1f;

        public string Id => _id;
        public AudioClip Clip => _clip;
        public string Description => _description;
        public float DefaultVolume => _defaultVolume;
        public float DefaultPitch => _defaultPitch;
    }
}