using System;
using GameFramework.Audio.Data;
using UnityEngine;

namespace GameFramework.EventSystem.Events
{
    public class AudioEvents
    {
        /// <summary>
        /// Request to play background music
        /// </summary>
        public class PlayMusicEvent
        {
            public string MusicId { get; }
            public bool FadeIn { get; }
            public float FadeTime { get; }
            public bool Loop { get; }

            public PlayMusicEvent(string musicId, bool fadeIn = false, float fadeTime = 1f, bool loop = true)
            {
                MusicId = musicId ?? throw new ArgumentNullException(nameof(musicId));
                FadeIn = fadeIn;
                FadeTime = fadeTime;
                Loop = loop;
            }
        }

        /// <summary>
        /// Request to stop currently playing music
        /// </summary>
        public class StopMusicEvent
        {
            public bool FadeOut { get; }
            public float FadeTime { get; }

            public StopMusicEvent(bool fadeOut = false, float fadeTime = 1f)
            {
                FadeOut = fadeOut;
                FadeTime = fadeTime;
            }
        }

        /// <summary>
        /// Request to play a sound effect
        /// </summary>
        public class PlaySoundEvent
        {
            public string SoundId { get; }
            public float Volume { get; }
            public float Pitch { get; }
            public Vector3? Position { get; }

            public PlaySoundEvent(string soundId, float volume = 1f, float pitch = 1f, Vector3? position = null)
            {
                SoundId = soundId ?? throw new ArgumentNullException(nameof(soundId));
                Volume = Mathf.Clamp01(volume);
                Pitch = pitch;
                Position = position;
            }
        }

        /// <summary>
        /// Request to stop a specific sound effect
        /// </summary>
        public class StopSoundEvent
        {
            public string SoundId { get; }

            public StopSoundEvent(string soundId)
            {
                SoundId = soundId ?? throw new ArgumentNullException(nameof(soundId));
            }
        }

        /// <summary>
        /// UI-specific audio events for common interactions
        /// </summary>
        public class UIAudioEvent
        {
            public UIAudioType AudioType { get; }
            public string CustomSoundId { get; }

            public UIAudioEvent(UIAudioType audioType, string customSoundId = null)
            {
                AudioType = audioType;
                CustomSoundId = customSoundId;
            }
        }
    }
}