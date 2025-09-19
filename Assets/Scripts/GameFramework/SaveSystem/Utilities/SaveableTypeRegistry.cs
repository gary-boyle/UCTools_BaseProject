using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using GameFramework.SaveSystem.Attributes;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem;

namespace GameFramework.SaveSystem.Utilities
{
    /// <summary>
    /// Registry for automatic discovery and management of SaveableBase types and their associated save data types.
    /// Uses reflection to scan for SaveableType attributes and builds lookup tables for efficient type resolution.
    /// </summary>
    public static class SaveableTypeRegistry
    {
        #region Private Fields
        private static Dictionary<string, Type> _saveDataTypes = new Dictionary<string, Type>();
        private static Dictionary<Type, Type> _saveableToSaveDataTypes = new Dictionary<Type, Type>();
        private static Dictionary<Type, string> _saveDataToTypeNames = new Dictionary<Type, string>();
        private static bool _isInitialized = false;
        private static readonly object _lockObject = new object();
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets all registered type names
        /// </summary>
        public static string[] RegisteredTypeNames => _saveDataTypes.Keys.ToArray();
        
        /// <summary>
        /// Gets all registered SaveableBase types
        /// </summary>
        public static Type[] RegisteredSaveableTypes => _saveableToSaveDataTypes.Keys.ToArray();
        
        /// <summary>
        /// Gets all registered RuntimeObjectSaveData types
        /// </summary>
        public static Type[] RegisteredSaveDataTypes => _saveDataTypes.Values.ToArray();
        
        /// <summary>
        /// Gets the total count of registered types
        /// </summary>
        public static int RegisteredCount => _saveDataTypes.Count;
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the registry by scanning all assemblies for SaveableBase classes with SaveableType attributes.
        /// Called automatically on first use, but can be called manually to force re-initialization.
        /// </summary>
        public static void Initialize()
        {
            lock (_lockObject)
            {
                if (_isInitialized) return;
                
                Debug.Log("[SaveableTypeRegistry] Initializing saveable type registry...");
                
                try
                {
                    // Clear existing data
                    _saveDataTypes.Clear();
                    _saveableToSaveDataTypes.Clear();
                    _saveDataToTypeNames.Clear();
                    
                    // Scan all loaded assemblies for SaveableBase types
                    var saveableBaseType = typeof(SaveableBase);
                    var discoveredTypes = 0;
                    
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foreach (var type in assembly.GetTypes())
                            {
                                // Skip if not a SaveableBase subclass
                                if (!saveableBaseType.IsAssignableFrom(type) || type.IsAbstract)
                                    continue;
                                
                                // Look for SaveableType attribute
                                var attribute = type.GetCustomAttribute<SaveableTypeAttribute>();
                                if (attribute == null) continue;
                                
                                RegisterType(type, attribute);
                                discoveredTypes++;
                            }
                        }
                        catch (ReflectionTypeLoadException ex)
                        {
                            // Some assemblies may have types that can't be loaded, skip them
                            Debug.LogWarning($"[SaveableTypeRegistry] Could not load types from assembly {assembly.FullName}: {ex.Message}");
                            continue;
                        }
                    }
                    
                    _isInitialized = true;
                    Debug.Log($"[SaveableTypeRegistry] Registry initialized successfully. Discovered {discoveredTypes} saveable types.");
                    
                    // Log registered types for debugging
                    if (discoveredTypes > 0)
                    {
                        var typeNames = string.Join(", ", _saveDataTypes.Keys);
                        Debug.Log($"[SaveableTypeRegistry] Registered types: {typeNames}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveableTypeRegistry] Failed to initialize registry: {ex.Message}");
                    _isInitialized = false;
                }
            }
        }
        
        /// <summary>
        /// Forces re-initialization of the registry (useful for hot-reloading scenarios)
        /// </summary>
        public static void ForceReinitialize()
        {
            lock (_lockObject)
            {
                _isInitialized = false;
                Initialize();
            }
        }
        #endregion

        #region Type Lookup Methods
        /// <summary>
        /// Gets the RuntimeObjectSaveData type associated with a type name
        /// </summary>
        /// <param name="typeName">The type name (e.g., "ClickableCube")</param>
        /// <returns>The RuntimeObjectSaveData Type, or null if not found</returns>
        public static Type GetSaveDataType(string typeName)
        {
            EnsureInitialized();
            
            if (string.IsNullOrEmpty(typeName))
                return null;
                
            _saveDataTypes.TryGetValue(typeName, out var type);
            return type;
        }
        
        /// <summary>
        /// Gets the RuntimeObjectSaveData type associated with a SaveableBase type
        /// </summary>
        /// <param name="saveableType">The SaveableBase type</param>
        /// <returns>The RuntimeObjectSaveData Type, or null if not found</returns>
        public static Type GetSaveDataType(Type saveableType)
        {
            EnsureInitialized();
            
            if (saveableType == null)
                return null;
                
            _saveableToSaveDataTypes.TryGetValue(saveableType, out var type);
            return type;
        }
        
        /// <summary>
        /// Gets the type name associated with a RuntimeObjectSaveData type
        /// </summary>
        /// <param name="saveDataType">The RuntimeObjectSaveData type</param>
        /// <returns>The type name string, or null if not found</returns>
        public static string GetTypeName(Type saveDataType)
        {
            EnsureInitialized();
            
            if (saveDataType == null)
                return null;
                
            _saveDataToTypeNames.TryGetValue(saveDataType, out var typeName);
            return typeName;
        }
        
        /// <summary>
        /// Checks if a type name is registered
        /// </summary>
        /// <param name="typeName">The type name to check</param>
        /// <returns>True if registered, false otherwise</returns>
        public static bool IsTypeRegistered(string typeName)
        {
            EnsureInitialized();
            return !string.IsNullOrEmpty(typeName) && _saveDataTypes.ContainsKey(typeName);
        }
        
        /// <summary>
        /// Checks if a SaveableBase type is registered
        /// </summary>
        /// <param name="saveableType">The SaveableBase type to check</param>
        /// <returns>True if registered, false otherwise</returns>
        public static bool IsTypeRegistered(Type saveableType)
        {
            EnsureInitialized();
            return saveableType != null && _saveableToSaveDataTypes.ContainsKey(saveableType);
        }
        #endregion

        #region Factory Methods
        /// <summary>
        /// Creates a new instance of RuntimeObjectSaveData for the given type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>New RuntimeObjectSaveData instance, or null if type not found</returns>
        public static RuntimeObjectSaveData CreateSaveDataInstance(string typeName)
        {
            var saveDataType = GetSaveDataType(typeName);
            if (saveDataType == null)
            {
                Debug.LogWarning($"[SaveableTypeRegistry] No save data type registered for: {typeName}");
                return null;
            }
            
            try
            {
                return (RuntimeObjectSaveData)Activator.CreateInstance(saveDataType);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveableTypeRegistry] Failed to create instance of {saveDataType.Name}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates a new instance of RuntimeObjectSaveData for the given SaveableBase type
        /// </summary>
        /// <param name="saveableType">The SaveableBase type</param>
        /// <returns>New RuntimeObjectSaveData instance, or null if type not found</returns>
        public static RuntimeObjectSaveData CreateSaveDataInstance(Type saveableType)
        {
            var saveDataType = GetSaveDataType(saveableType);
            if (saveDataType == null)
            {
                Debug.LogWarning($"[SaveableTypeRegistry] No save data type registered for SaveableBase type: {saveableType?.Name}");
                return null;
            }
            
            try
            {
                return (RuntimeObjectSaveData)Activator.CreateInstance(saveDataType);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveableTypeRegistry] Failed to create instance of {saveDataType.Name}: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Private Methods
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
                Initialize();
        }
        
        private static void RegisterType(Type saveableType, SaveableTypeAttribute attribute)
        {
            // Generate type name from attribute or SaveableBase class name
            string typeName = !string.IsNullOrEmpty(attribute.DisplayName) 
                ? attribute.DisplayName 
                : GetDefaultTypeName(saveableType, attribute.SaveDataType);
            
            // Check for conflicts
            if (_saveDataTypes.ContainsKey(typeName))
            {
                Debug.LogError($"[SaveableTypeRegistry] Type name '{typeName}' is already registered. " +
                             $"Existing: {_saveDataTypes[typeName].Name}, New: {attribute.SaveDataType.Name}");
                return;
            }
            
            if (_saveableToSaveDataTypes.ContainsKey(saveableType))
            {
                Debug.LogError($"[SaveableTypeRegistry] SaveableBase type '{saveableType.Name}' is already registered.");
                return;
            }
            
            // Register in all lookup tables
            _saveDataTypes[typeName] = attribute.SaveDataType;
            _saveableToSaveDataTypes[saveableType] = attribute.SaveDataType;
            _saveDataToTypeNames[attribute.SaveDataType] = typeName;
            
            Debug.Log($"[SaveableTypeRegistry] Registered: {saveableType.Name} -> {typeName} ({attribute.SaveDataType.Name})");
        }
        
        private static string GetDefaultTypeName(Type saveableType, Type saveDataType)
        {
            // Try to extract a clean name from the SaveableBase type name
            string typeName = saveableType.Name;
            
            // Remove common suffixes that might be in the class name
            string[] suffixesToRemove = { "Saveable", "Component", "Behaviour" };
            foreach (var suffix in suffixesToRemove)
            {
                if (typeName.EndsWith(suffix))
                {
                    typeName = typeName.Substring(0, typeName.Length - suffix.Length);
                    break;
                }
            }
            
            return typeName;
        }
        #endregion

        #region Debug and Utility Methods
        /// <summary>
        /// Logs detailed information about all registered types (for debugging)
        /// </summary>
        public static void LogRegisteredTypes()
        {
            EnsureInitialized();
            
            Debug.Log($"[SaveableTypeRegistry] === Registered Types ({RegisteredCount}) ===");
            
            foreach (var kvp in _saveDataTypes)
            {
                var typeName = kvp.Key;
                var saveDataType = kvp.Value;
                
                // Find the corresponding SaveableBase type
                var saveableType = _saveableToSaveDataTypes.FirstOrDefault(x => x.Value == saveDataType).Key;
                
                Debug.Log($"[SaveableTypeRegistry] '{typeName}' -> {saveableType?.Name} -> {saveDataType.Name}");
            }
            
            Debug.Log("[SaveableTypeRegistry] === End Registered Types ===");
        }
        
        /// <summary>
        /// Validates that all registered types are properly configured
        /// </summary>
        /// <returns>True if all types are valid, false if there are issues</returns>
        public static bool ValidateRegisteredTypes()
        {
            EnsureInitialized();
            
            bool allValid = true;
            
            foreach (var kvp in _saveDataTypes)
            {
                var typeName = kvp.Key;
                var saveDataType = kvp.Value;
                
                // Check if saveDataType has a parameterless constructor
                if (saveDataType.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogError($"[SaveableTypeRegistry] Save data type '{saveDataType.Name}' must have a parameterless constructor");
                    allValid = false;
                }
                
                // Check if saveDataType properly inherits from RuntimeObjectSaveData
                if (!typeof(RuntimeObjectSaveData).IsAssignableFrom(saveDataType))
                {
                    Debug.LogError($"[SaveableTypeRegistry] Save data type '{saveDataType.Name}' must inherit from RuntimeObjectSaveData");
                    allValid = false;
                }
            }
            
            return allValid;
        }
        #endregion
    }
}
