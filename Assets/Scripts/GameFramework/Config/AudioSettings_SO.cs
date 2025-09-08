using System.Collections.Generic;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GrameFramework.Config
{
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "Config Variables/Audio Settings")]
    public class AudioSettings_SO : ConfigCategory
    {
        [Header("Master Controls")]
        public BoolConfigVariable audioEnabled = new BoolConfigVariable(
            "audio.enabled", 
            "Master control to enable or disable all audio", 
            true, 
            ConfigFlags.Save);
            
        public IntConfigVariable masterVolume = new IntConfigVariable(
            "audio.master_volume", 
            "Master volume level (1 - 100)", 
            100, 
            ConfigFlags.Save);

        [Header("Volume Levels")]
        public IntConfigVariable musicVolume = new IntConfigVariable(
            "audio.music_volume", 
            "Music volume level (1 - 100)", 
            80, 
            ConfigFlags.Save);
            
        public IntConfigVariable sfxVolume = new IntConfigVariable(
            "audio.sfx_volume", 
            "SFX volume level (1 - 100)", 
            90, 
            ConfigFlags.Save);
        
        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                audioEnabled,
                masterVolume,
                musicVolume,
                sfxVolume
            };
        }

        /// <summary>
        /// Apply audio enabled state change with immediate effect
        /// </summary>
        public void SetAudioEnabled(bool enabled)
        {
            audioEnabled.Value = enabled;
            
            // Apply audio muting/unmuting logic here
            AudioListener.volume = enabled ? (masterVolume.Value / 100f) : 0f;
            
            Debug.Log($"[AudioSettings] Audio enabled: {enabled}");
        }

        /// <summary>
        /// Apply master volume change with immediate effect
        /// </summary>
        public void SetMasterVolume(int volume)
        {
            volume = Mathf.Clamp(volume, 0, 100);
            masterVolume.Value = volume;
            
            // Apply master volume if audio is enabled
            if (audioEnabled.Value)
            {
                AudioListener.volume = volume / 100f;
            }
            
            Debug.Log($"[AudioSettings] Master volume: {volume}");
        }

        /// <summary>
        /// Apply music volume change with immediate effect
        /// </summary>
        public void SetMusicVolume(int volume)
        {
            volume = Mathf.Clamp(volume, 0, 100);
            musicVolume.Value = volume;
            
            // Apply to music audio sources/mixer groups
            // Example: AudioMixer.SetFloat("MusicVolume", Mathf.Log10(volume / 100f) * 20);
            
            Debug.Log($"[AudioSettings] Music volume: {volume}");
        }

        /// <summary>
        /// Apply SFX volume change with immediate effect
        /// </summary>
        public void SetSfxVolume(int volume)
        {
            volume = Mathf.Clamp(volume, 0, 100);
            sfxVolume.Value = volume;
            
            // Apply to SFX audio sources/mixer groups
            // Example: AudioMixer.SetFloat("SFXVolume", Mathf.Log10(volume / 100f) * 20);
            
            Debug.Log($"[AudioSettings] SFX volume: {volume}");
        }
    }
}
