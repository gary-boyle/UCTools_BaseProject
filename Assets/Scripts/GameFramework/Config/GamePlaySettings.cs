using System.Collections.Generic;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GrameFramework.Config
{
    [CreateAssetMenu(fileName = "GameplaySettings", menuName = "Config Variables/Gameplay Settings")]
    public class GameplaySettings : ConfigCategory
    {
        [Header("Difficulty")]
        public IntConfigVariable difficulty = new IntConfigVariable(
            "game.difficulty", 
            "Game difficulty (0 = easy, 1 = normal, 2 = hard)", 
            1, 
            ConfigFlags.Save,
            minValue: 0,
            maxValue: 2);

        [Header("Save System")]
        public BoolConfigVariable autoSave = new BoolConfigVariable(
            "game.auto_save", 
            "Auto-save enabled", 
            true, 
            ConfigFlags.Save);
            
        public IntConfigVariable autoSaveInterval = new IntConfigVariable(
            "game.auto_save_interval", 
            "Auto-save interval in seconds", 
            300, 
            ConfigFlags.Save,
            minValue: 30,     // Minimum 30 seconds
            maxValue: 3600);  // Maximum 1 hour
        
        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                difficulty,
                autoSave,
                autoSaveInterval
            };
        }
    }
}