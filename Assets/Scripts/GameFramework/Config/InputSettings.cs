using System.Collections.Generic;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GrameFramework.Config
{
    [CreateAssetMenu(fileName = "InputSettings", menuName = "Config Variables/Input Settings")]
    public class InputSettings : ConfigCategory
    {
        [Header("Mouse Settings")]
        public FloatConfigVariable mouseSensitivity = new FloatConfigVariable(
            "input.mouse_sensitivity", 
            "Mouse sensitivity multiplier", 
            1.0f, 
            ConfigFlags.Save,
            minValue: 0.1f,
            maxValue: 10f);
            
        public BoolConfigVariable invertYAxis = new BoolConfigVariable(
            "input.invert_y_axis", 
            "Invert Y axis", 
            false, 
            ConfigFlags.Save);
        
        public override List<ConfigVariableBase> GetAllVariables()
        {
            return new List<ConfigVariableBase>
            {
                mouseSensitivity,
                invertYAxis
            };
        }
        
    }
}