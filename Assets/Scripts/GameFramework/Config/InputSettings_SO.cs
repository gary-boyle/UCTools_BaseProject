using System.Collections.Generic;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GrameFramework.Config
{
    [CreateAssetMenu(fileName = "InputSettings", menuName = "Config Variables/Input Settings")]
    public class InputSettings_SO : ConfigCategory
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

        /// <summary>
        /// Apply mouse sensitivity setting
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            sensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            mouseSensitivity.Value = sensitivity;
            
            // Apply to input system or camera controller
            // Example: CameraController.Instance.SetMouseSensitivity(sensitivity);
            
            Debug.Log($"[InputSettings] Mouse sensitivity: {sensitivity:F2}");
        }

        /// <summary>
        /// Apply Y-axis inversion setting
        /// </summary>
        public void SetInvertYAxis(bool invert)
        {
            invertYAxis.Value = invert;
            
            // Apply to input system or camera controller
            // Example: CameraController.Instance.SetInvertYAxis(invert);
            
            Debug.Log($"[InputSettings] Invert Y axis: {invert}");
        }
    }
}