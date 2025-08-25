// using System;
// using System.Text.RegularExpressions;
// using UnityEngine;
//
// namespace UCTools_ConfigVariables
// {
//     /// <summary>
//     /// Serializable configuration variable that can be embedded in ScriptableObjects
//     /// Handles validation, type conversion, and change tracking
//     /// </summary>
//     [System.Serializable]
//     public class ConfigVariable
//     {
//         [Header("Variable Definition")]
//         public string name = "";
//         public string description = "";
//         public string defaultValue = "";
//         public ConfigFlags flags = ConfigFlags.None;
//         
//         [Header("Value Constraints (Optional)")]
//         public bool hasMinValue = false;
//         public float minValue = 0f;
//         public bool hasMaxValue = false;
//         public float maxValue = 100f;
//         
//         [Header("Runtime State")]
//         [SerializeField] private string currentValue = "";
//         
//         // Cached converted values
//         [System.NonSerialized] private bool _cacheValid = false;
//         [System.NonSerialized] private int _cachedIntValue = 0;
//         [System.NonSerialized] private float _cachedFloatValue = 0f;
//         [System.NonSerialized] private bool _cachedBoolValue = false;
//         [System.NonSerialized] private bool _hasChanged = false;
//         
//         // Name validation regex
//         private static readonly Regex s_nameValidationRegex = new Regex(@"^[a-z_+-][a-z0-9_+.-]*$");
//         
//         /// <summary>Current runtime value with validation</summary>
//         public string CurrentValue
//         {
//             get => string.IsNullOrEmpty(currentValue) ? defaultValue : currentValue;
//             set
//             {
//                 if (ValidateValue(value))
//                 {
//                     if (currentValue != value)
//                     {
//                         currentValue = value;
//                         InvalidateCache();
//                         _hasChanged = true;
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"Invalid value '{value}' for config variable '{name}'. Using current value.");
//                 }
//             }
//         }
//         
//         /// <summary>Get current value as integer</summary>
//         public int IntValue
//         {
//             get
//             {
//                 ValidateCache();
//                 return _cachedIntValue;
//             }
//         }
//         
//         /// <summary>Get current value as float</summary>
//         public float FloatValue
//         {
//             get
//             {
//                 ValidateCache();
//                 return _cachedFloatValue;
//             }
//         }
//         
//         /// <summary>Get current value as boolean (0 = false, anything else = true)</summary>
//         public bool BoolValue
//         {
//             get
//             {
//                 ValidateCache();
//                 return _cachedBoolValue;
//             }
//         }
//         
//         /// <summary>Check if the variable has been modified since the last call to ChangeCheck()</summary>
//         public bool ChangeCheck()
//         {
//             if (!_hasChanged)
//                 return false;
//                 
//             _hasChanged = false;
//             return true;
//         }
//         
//         /// <summary>Reset to default value</summary>
//         public void ResetToDefault()
//         {
//             CurrentValue = defaultValue;
//         }
//         
//         /// <summary>Validate the configuration variable setup</summary>
//         public bool ValidateConfiguration(out string error)
//         {
//             error = "";
//             
//             // Validate name format
//             if (string.IsNullOrEmpty(name))
//             {
//                 error = "Name cannot be empty";
//                 return false;
//             }
//             
//             if (!s_nameValidationRegex.IsMatch(name))
//             {
//                 error = $"Invalid name format: '{name}'. Must match pattern: {s_nameValidationRegex}";
//                 return false;
//             }
//             
//             // Validate default value
//             if (!ValidateValue(defaultValue))
//             {
//                 error = $"Invalid default value: '{defaultValue}'";
//                 return false;
//             }
//             
//             return true;
//         }
//         
//         /// <summary>Validate a value against constraints</summary>
//         private bool ValidateValue(string value)
//         {
//             if (string.IsNullOrEmpty(value))
//                 return false;
//             
//             // For numeric constraints, try to parse and check bounds
//             if (hasMinValue || hasMaxValue)
//             {
//                 if (float.TryParse(value, System.Globalization.NumberStyles.Float,
//                     System.Globalization.CultureInfo.InvariantCulture, out float numValue))
//                 {
//                     if (hasMinValue && numValue < minValue)
//                         return false;
//                     if (hasMaxValue && numValue > maxValue)
//                         return false;
//                 }
//                 else
//                 {
//                     // If we have numeric constraints but can't parse as number, it's invalid
//                     return false;
//                 }
//             }
//             
//             return true;
//         }
//         
//         /// <summary>Update cached converted values</summary>
//         private void ValidateCache()
//         {
//             if (_cacheValid)
//                 return;
//                 
//             string value = CurrentValue;
//             
//             // Parse integer
//             if (!int.TryParse(value, out _cachedIntValue))
//                 _cachedIntValue = 0;
//                 
//             // Parse float
//             if (!float.TryParse(value, System.Globalization.NumberStyles.Float,
//                 System.Globalization.CultureInfo.InvariantCulture, out _cachedFloatValue))
//                 _cachedFloatValue = 0f;
//                 
//             // Parse boolean (0 = false, anything else = true)
//             _cachedBoolValue = _cachedIntValue != 0;
//             
//             _cacheValid = true;
//         }
//         
//         /// <summary>Invalidate cached values when value changes</summary>
//         private void InvalidateCache()
//         {
//             _cacheValid = false;
//         }
//         
//         /// <summary>Constructor for code creation</summary>
//         public ConfigVariable(string name, string description, string defaultValue, ConfigFlags flags = ConfigFlags.None)
//         {
//             this.name = name;
//             this.description = description;
//             this.defaultValue = defaultValue;
//             this.flags = flags;
//             this.currentValue = "";
//         }
//         
//         /// <summary>Default constructor for Unity serialization</summary>
//         public ConfigVariable() { }
//     }
// }
