using System.Collections.Generic;
using GameFramework.Config.Enums;
using GameFramework.Config.Variables;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameFramework.Config.ScriptableObjects
{
    /// <summary>
    /// Simplified audio settings that just manages data and publishes change events
    /// All audio application logic moved to AudioService
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "Config Variables/Audio Settings")]
    public class AudioSettings_SO : ConfigCategoryBase
    {
        [Header("Master Controls")]
        public BoolConfigVariable AudioEnabled = new BoolConfigVariable(
            "audio.enabled", 
            "Master control to enable or disable all audio", 
            true, 
            ConfigFlags.Save);

        [Header("Volume Levels (0.0 - 1.0)")]
        public FloatConfigVariable MasterVolume = new FloatConfigVariable(
            "audio.master_volume", 
            "Master volume level (0.0 - 1.0)", 
            1.0f, 
            ConfigFlags.Save);

        public FloatConfigVariable MusicVolume = new FloatConfigVariable(
            "audio.music_volume", 
            "Music volume level (0.0 - 1.0)", 
            0.8f, 
            ConfigFlags.Save);
            
        public FloatConfigVariable SFXVolume = new FloatConfigVariable(
            "audio.sfx_volume", 
            "SFX volume level (0.0 - 1.0)", 
            0.9f, 
            ConfigFlags.Save);

        public FloatConfigVariable UIVolume = new FloatConfigVariable(
            "audio.ui_volume", 
            "UI volume level (0.0 - 1.0)", 
            1.0f, 
            ConfigFlags.Save);

        public override ConfigTypes CategoryType => ConfigTypes.Audio;

        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                AudioEnabled,
                MasterVolume,
                MusicVolume,
                SFXVolume,
                UIVolume
            };
        }

        /// <summary>
        /// Set audio enabled and publish change event
        /// </summary>
        public void SetAudioEnabled(bool enabled)
        {
            if (AudioEnabled.Value != enabled)
            {
                AudioEnabled.Value = enabled;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set master volume and publish change event
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Abs(MasterVolume.Value - volume) > 0.001f)
            {
                MasterVolume.Value = volume;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set music volume and publish change event
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Abs(MusicVolume.Value - volume) > 0.001f)
            {
                MusicVolume.Value = volume;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set SFX volume and publish change event
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Abs(SFXVolume.Value - volume) > 0.001f)
            {
                SFXVolume.Value = volume;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set UI volume and publish change event
        /// </summary>
        public void SetUIVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (Mathf.Abs(UIVolume.Value - volume) > 0.001f)
            {
                UIVolume.Value = volume;
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
        public int GetMasterVolumeAsPercentage() => Mathf.RoundToInt(MasterVolume.Value * 100f);
        public int GetMusicVolumeAsPercentage() => Mathf.RoundToInt(MusicVolume.Value * 100f);
        public int GetSfxVolumeAsPercentage() => Mathf.RoundToInt(SFXVolume.Value * 100f);
        public int GetUIVolumeAsPercentage() => Mathf.RoundToInt(UIVolume.Value * 100f);

        /// <summary>
        /// Set volume from percentage (0-100) for UI convenience
        /// </summary>
        public void SetMasterVolumeFromPercentage(int percentage) => SetMasterVolume(percentage / 100f);
        public void SetMusicVolumeFromPercentage(int percentage) => SetMusicVolume(percentage / 100f);
        public void SetSfxVolumeFromPercentage(int percentage) => SetSfxVolume(percentage / 100f);
        public void SetUIVolumeFromPercentage(int percentage) => SetUIVolume(percentage / 100f);
    }
}
