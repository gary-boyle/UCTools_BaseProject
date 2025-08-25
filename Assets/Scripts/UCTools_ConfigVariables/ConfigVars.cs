// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using UnityEngine;
//
// namespace UCTools_ConfigVariables
// {
//     /// <summary>
//     /// Static registry and management system for config variables
//     /// Works with the new ScriptableObject-based system
//     /// </summary>
//     public static class ConfigVar
//     {
//         private static Dictionary<string, ConfigVariableBase> s_configVars =
//             new Dictionary<string, ConfigVariableBase>();
//
//         private static bool s_initialized = false;
//
//         /// <summary>
//         /// Dictionary of all registered config variables by name
//         /// </summary>
//         public static IReadOnlyDictionary<string, ConfigVariableBase> ConfigVars => s_configVars;
//
//         /// <summary>
//         /// Initialize the config variable system
//         /// </summary>
//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
//         public static void Initialize()
//         {
//             if (s_initialized) return;
//
//             s_configVars.Clear();
//
//             // Find and register all ConfigVarAttribute fields
//             //RegisterAttributeFields();
//
//             // Find and register all ConfigCategory ScriptableObjects
//             RegisterConfigCategories();
//
//             s_initialized = true;
//             Debug.Log($"ConfigVar system initialized with {s_configVars.Count} variables");
//         }
//
//         /// <summary>
//         /// Reset statics when domain reload is disabled
//         /// </summary>
//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
//         private static void ResetStatics()
//         {
//             s_configVars.Clear();
//             s_initialized = false;
//         }
//
//         /// <summary>
//         /// Register a config variable
//         /// </summary>
//         public static void Register(ConfigVariableBase configVar)
//         {
//             if (configVar == null || string.IsNullOrEmpty(configVar.name))
//             {
//                 Debug.LogWarning("Cannot register null or unnamed config variable");
//                 return;
//             }
//
//             if (s_configVars.ContainsKey(configVar.name))
//             {
//                 Debug.LogWarning($"Config variable '{configVar.name}' already registered. Overriding.");
//             }
//
//             s_configVars[configVar.name] = configVar;
//         }
//
//         /// <summary>
//         /// Get a config variable by name
//         /// </summary>
//         public static ConfigVariableBase Get(string name)
//         {
//             s_configVars.TryGetValue(name, out var configVar);
//             return configVar;
//         }
//
//         /// <summary>
//         /// Get a typed config variable
//         /// </summary>
//         public static T Get<T>(string name) where T : ConfigVariableBase
//         {
//             return Get(name) as T;
//         }
//
//         /// <summary>
//         /// Check if a config variable exists
//         /// </summary>
//         public static bool Exists(string name)
//         {
//             return s_configVars.ContainsKey(name);
//         }
//
//         /// <summary>
//         /// Set a config variable value from string
//         /// </summary>
//         public static bool SetValue(string name, string value)
//         {
//             var configVar = Get(name);
//             if (configVar == null) return false;
//
//             return configVar.SetValueFromString(value);
//         }
//
//         /// <summary>
//         /// Get a config variable value as string
//         /// </summary>
//         public static string GetValue(string name)
//         {
//             var configVar = Get(name);
//             return configVar?.GetValueAsString() ?? "";
//         }
//
//         private static void RegisterAttributeFields()
//         {
//             var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
//
//             foreach (var assembly in assemblies)
//             {
//                 try
//                 {
//                     var types = assembly.GetTypes();
//                     foreach (var type in types)
//                     {
//                         var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
//                         foreach (var field in fields)
//                         {
//                             var attribute = field.GetCustomAttribute<ConfigVarAttribute>();
//                             if (attribute != null)
//                             {
//                                 RegisterAttributeField(field, attribute);
//                             }
//                         }
//                     }
//                 }
//                 catch (Exception e)
//                 {
//                     Debug.LogWarning($"Error scanning assembly {assembly.FullName}: {e.Message}");
//                 }
//             }
//         }
//
//         private static void RegisterAttributeField(FieldInfo field, ConfigVarAttribute attribute)
//         {
//             try
//             {
//                 // Create appropriate config variable based on field type or default value
//                 ConfigVariableBase configVar = CreateConfigVariableFromAttribute(attribute);
//
//                 if (configVar != null)
//                 {
//                     // Set the field value
//                     field.SetValue(null, configVar);
//
//                     // Register the variable
//                     Register(configVar);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Error registering config variable for field {field.Name}: {e.Message}");
//             }
//         }
//
//         private static ConfigVariableBase CreateConfigVariableFromAttribute(ConfigVarAttribute attribute)
//         {
//             // Try to infer type from default value
//             if (bool.TryParse(attribute.DefaultValue, out bool boolVal))
//             {
//                 return new BoolConfigVariable(attribute.Name, attribute.Description, boolVal, attribute.Flags);
//             }
//
//             if (int.TryParse(attribute.DefaultValue, out int intVal))
//             {
//                 return new IntConfigVariable(attribute.Name, attribute.Description, intVal, attribute.Flags);
//             }
//
//             if (float.TryParse(attribute.DefaultValue, out float floatVal))
//             {
//                 return new FloatConfigVariable(attribute.Name, attribute.Description, floatVal, attribute.Flags);
//             }
//
//             // Default to string
//             return new StringConfigVariable(attribute.Name, attribute.Description, attribute.DefaultValue,
//                 attribute.Flags);
//         }
//
//         private static void RegisterConfigCategories()
//         {
//             // Find all ConfigCategory assets in the project
//             var configCategories = Resources.FindObjectsOfTypeAll<ConfigCategory>();
//
//             foreach (var category in configCategories)
//             {
//                 var variables = category.GetAllVariables();
//                 foreach (var variable in variables)
//                 {
//                     Register(variable);
//                 }
//             }
//         }
//     }
// }
// //     /// <summary>
// //     /// Attribute for marking static fields as config variables
// //     /// </summary>
// //     [System.AttributeUsage(System.AttributeTargets.Field)]
// //     public class ConfigVarAttribute : System.Attribute
// //     {
// //         public string Name { get; set; } = "";
// //         public string Description { get; set; } = "";
// //         public string DefaultValue { get; set; } = "";
// //         public ConfigFlags Flags { get; set; } = ConfigFlags.None;
// //     }
// // }
