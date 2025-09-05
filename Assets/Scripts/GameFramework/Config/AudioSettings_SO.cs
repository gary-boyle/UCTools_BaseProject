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
    }
}
