using System.Collections.Generic;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;
using UCTools_ConfigVariables;
using UnityEngine;

namespace GameFramework.Config
{
    /// <summary>
    /// Simplified input settings that just manages data and publishes change events
    /// All input application logic moved to InputManager or relevant services
    /// </summary>
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
        /// Set mouse sensitivity and publish change event
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            sensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
            if (Mathf.Abs(mouseSensitivity.Value - sensitivity) > 0.001f)
            {
                mouseSensitivity.Value = sensitivity;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Set Y-axis inversion and publish change event
        /// </summary>
        public void SetInvertYAxis(bool invert)
        {
            if (invertYAxis.Value != invert)
            {
                invertYAxis.Value = invert;
                PublishOptionsChangedEvent();
            }
        }

        /// <summary>
        /// Publish options changed event to notify relevant services
        /// </summary>
        private void PublishOptionsChangedEvent()
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new OptionsChangedEvent());
        }

        /// <summary>
        /// Get mouse sensitivity as percentage (10-1000%) for UI display
        /// </summary>
        public int GetMouseSensitivityAsPercentage()
        {
            return Mathf.RoundToInt(mouseSensitivity.Value * 100f);
        }

        /// <summary>
        /// Set mouse sensitivity from percentage (10-1000%) for UI convenience
        /// </summary>
        public void SetMouseSensitivityFromPercentage(int percentage)
        {
            float sensitivity = Mathf.Clamp(percentage / 100f, 0.1f, 10f);
            SetMouseSensitivity(sensitivity);
        }

        /// <summary>
        /// Get current mouse sensitivity with proper clamping
        /// </summary>
        public float GetMouseSensitivity()
        {
            return Mathf.Clamp(mouseSensitivity.Value, 0.1f, 10f);
        }

        /// <summary>
        /// Get current Y-axis inversion setting
        /// </summary>
        public bool GetInvertYAxis()
        {
            return invertYAxis.Value;
        }

        /// <summary>
        /// Reset mouse settings to defaults
        /// </summary>
        public void ResetMouseSettings()
        {
            SetMouseSensitivity(1.0f);
            SetInvertYAxis(false);
        }
    }
}
