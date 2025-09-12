using System.Collections.Generic;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GameFramework.Config
{
    /// <summary>
    /// Simplified audio settings that just manages data and publishes change events
    /// All audio application logic moved to AudioService
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "Config Variables/Audio Settings")]
    public class AudioSettings_SO : ConfigCategory
    {
        [Header("Master Controls")]
        public BoolConfigVariable audioEnabled = new BoolConfigVariable(
            "audio.enabled", 
            "Master control to enable or disable all audio", 
            true, 
            ConfigFlags.Save);

        [Header("Volume Levels (0.0 - 1.0)")]
        public FloatConfigVariable masterVolume = new FloatConfigVariable(
            "audio.master_volume", 
            "Master volume level (0.0 - 1.0)", 
            1.0f, 
            ConfigFlags.Save);

        public FloatConfigVariable musicVolume = new FloatConfigVariable(
            "audio.music_volume", 
            "Music volume level (0.0 - 1.0)", 
            0.8f, 
            ConfigFlags.Save);
            
        public FloatConfigVariable sfxVolume = new FloatConfigVariable(
            "audio.sfx_volume", 
            "SFX volume level (0.0 - 1.0)", 
            0.9f, 
            ConfigFlags.Save);

        public FloatConfigVariable uiVolume = new FloatConfigVariable(
            "audio.ui_volume", 
            "UI volume level (0.0 - 1.0)", 
            1.0f, 
            ConfigFlags.Save);

        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                audioEnabled,
                masterVolume,
                musicVolume,
                sfxVolume,
                uiVolume
            };
        }

        /// <summary>
        /// Set audio enabled and publish change event
        /// </summary>
        public void SetAudioEnabled(bool enabled)
        {
            if (audioEnabled.Value != enabled)
            {
                audioEnabled.Value = enabled;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set master volume and publish change event
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Abs(masterVolume.Value - volume) > 0.001f)
            {
                masterVolume.Value = volume;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set music volume and publish change event
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Abs(musicVolume.Value - volume) > 0.001f)
            {
                musicVolume.Value = volume;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set SFX volume and publish change event
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Abs(sfxVolume.Value - volume) > 0.001f)
            {
                sfxVolume.Value = volume;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set UI volume and publish change event
        /// </summary>
        public void SetUIVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Abs(uiVolume.Value - volume) > 0.001f)
            {
                uiVolume.Value = volume;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Publish options changed event to notify AudioService
        /// </summary>
        private void PublishOptionsChangedEvent()
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new OptionsChangedEvent());
        }

        /// <summary>
        /// Convert normalized volume (0-1) to percentage for UI display
        /// </summary>
        public int GetMasterVolumeAsPercentage() => Mathf.RoundToInt(masterVolume.Value * 100f);
        public int GetMusicVolumeAsPercentage() => Mathf.RoundToInt(musicVolume.Value * 100f);
        public int GetSfxVolumeAsPercentage() => Mathf.RoundToInt(sfxVolume.Value * 100f);
        public int GetUIVolumeAsPercentage() => Mathf.RoundToInt(uiVolume.Value * 100f);

        /// <summary>
        /// Set volume from percentage (0-100) for UI convenience
        /// </summary>
        public void SetMasterVolumeFromPercentage(int percentage) => SetMasterVolume(percentage / 100f);
        public void SetMusicVolumeFromPercentage(int percentage) => SetMusicVolume(percentage / 100f);
        public void SetSfxVolumeFromPercentage(int percentage) => SetSfxVolume(percentage / 100f);
        public void SetUIVolumeFromPercentage(int percentage) => SetUIVolume(percentage / 100f);
    }
}
