using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UCTools_ConfigVariables
{
    /// <summary>
    /// Configuration Variable system for managing persistent game settings and console variables
    /// 
    /// Design Philosophy:
    /// - Provides type-safe access to configuration values with automatic conversion
    /// - Supports automatic discovery via attributes for clean declaration
    /// - Handles persistence with selective saving based on flags
    /// - Integrates with console system for runtime modification
    /// - Validates variable names to ensure consistency and prevent conflicts
    /// 
    /// Usage Examples:
    /// 1. Declare via attribute:
    ///    [ConfigVar(Name = "player.health", DefaultValue = "100", Description = "Player max health")]
    ///    static ConfigVar playerHealth;
    /// 
    /// 2. Access values:
    ///    int health = playerHealth.IntValue;
    ///    float healthFloat = playerHealth.FloatValue;
    ///    string healthString = playerHealth.Value;
    /// 
    /// 3. Modify values:
    ///    playerHealth.Value = "150";
    /// 
    /// 4. Check for changes:
    ///    if (playerHealth.ChangeCheck()) { /* value was modified */ }
    /// </summary>
    public class ConfigVar
    {
        #region Static Members and Collections
        
        /// <summary>
        /// Global registry of all configuration variables in the system
        /// Key: variable name (lowercase), Value: ConfigVar instance
        /// This is the central store for all config variables across the entire application
        /// </summary>
        public static Dictionary<string, ConfigVar> ConfigVars;
        
        /// <summary>
        /// Tracks which types of variables have been modified since last save
        /// Used to optimize save operations - only save when variables with Save flag have changed
        /// Prevents unnecessary file I/O when only temporary/runtime variables are modified
        /// </summary>
        public static ConfigFlags DirtyFlags = ConfigFlags.None;

        /// <summary>
        /// Prevents double-initialization of the ConfigVar system
        /// Set to true after first Init() call to avoid re-scanning assemblies and duplicate registration
        /// </summary>
        static bool s_Initialized = false;

        #endregion

        #region Instance Fields
        
        /// <summary>
        /// The unique identifier for this configuration variable
        /// Must follow naming convention: lowercase letters, numbers, and limited special characters
        /// Typically uses dot notation for hierarchy: "player.health", "graphics.quality", etc.
        /// </summary>
        public readonly string name;
        
        /// <summary>
        /// Human-readable description of what this variable controls
        /// Displayed in help systems, documentation, and debugging tools
        /// Should be concise but descriptive enough for users to understand the purpose
        /// </summary>
        public readonly string description;
        
        /// <summary>
        /// The default value for this variable as a string
        /// Used when resetting to defaults or when variable is first created
        /// All values are stored as strings internally and converted as needed
        /// </summary>
        public readonly string defaultValue;
        
        /// <summary>
        /// Behavioral flags that control how this variable is handled
        /// Determines save behavior, network replication, cheat status, etc.
        /// Multiple flags can be combined using bitwise OR operations
        /// </summary>
        public readonly ConfigFlags flags;
        
        /// <summary>
        /// Indicates if the variable value has been modified since last ChangeCheck()
        /// Used for systems that need to react only when values actually change
        /// Automatically reset to false when ChangeCheck() is called
        /// </summary>
        public bool changed;

        /// <summary>Current string value of the variable</summary>
        string _stringValue;
        
        /// <summary>Cached float conversion of the current value (0 if conversion fails)</summary>
        float _floatValue;
        
        /// <summary>Cached integer conversion of the current value (0 if conversion fails)</summary>
        int _intValue;

        #endregion

        #region Static System Management
        
        /// <summary>
        /// Initialize the ConfigVar system and discover all variables marked with attributes
        /// Must be called once during application startup before using any ConfigVars
        /// 
        /// Process:
        /// 1. Creates the global ConfigVars dictionary
        /// 2. Scans all loaded assemblies for ConfigVarAttribute fields
        /// 3. Creates ConfigVar instances and registers them
        /// 4. Sets the static field references for easy access
        /// 
        /// Performance Note: Uses reflection to scan assemblies - only call once at startup
        /// </summary>
        public static void Init()
        {
            if (s_Initialized)
                return;

            ConfigVars = new Dictionary<string, ConfigVar>();
            InjectAttributeConfigVars();
            s_Initialized = true;
        }

        /// <summary>
        /// Reset all registered configuration variables back to their default values
        /// Useful for "Reset to Defaults" functionality in options menus
        /// Triggers change notifications for any variables that actually change
        /// Does not affect the DirtyFlags - changes are considered intentional user actions
        /// </summary>
        public static void ResetAllToDefault()
        {
            foreach (var v in ConfigVars)
            {
                v.Value.ResetToDefault();
            }
        }

        /// <summary>
        /// Save only configuration variables that have been modified and have the Save flag
        /// More efficient than Save() as it only writes when necessary
        /// Checks DirtyFlags to determine if any saveable variables have been modified
        /// Automatically clears the Save dirty flag after successful save
        /// </summary>
        /// <param name="filename">Path to the configuration file to write</param>
        public static void SaveChangedVars(string filename)
        {
            if ((DirtyFlags & ConfigFlags.Save) == ConfigFlags.None)
                return;

            Save(filename);
        }

        /// <summary>
        /// Save all configuration variables with the Save flag to a file
        /// Creates a plain text file with format: variablename "value"
        /// Can be executed from console or loaded back using standard config file parsing
        /// Overwrites existing file - use SaveChangedVars() for conditional saving
        /// </summary>
        /// <param name="filename">Path to the configuration file to write</param>
        public static void Save(string filename)
        {
            using (var st = System.IO.File.CreateText(filename))
            {
                foreach (var cvar in ConfigVars.Values)
                {
                    // Only save variables marked with Save flag
                    if ((cvar.flags & ConfigFlags.Save) == ConfigFlags.Save)
                        st.WriteLine("{0} \"{1}\"", cvar.name, cvar.Value);
                }

                // Clear the save dirty flag since we just saved
                DirtyFlags &= ~ConfigFlags.Save;
            }
        }

        /// <summary>
        /// Regular expression for validating configuration variable names
        /// Rules: Must start with lowercase letter, underscore, plus, or minus
        /// Can contain lowercase letters, numbers, underscore, plus, period, or minus
        /// This ensures consistent naming and prevents conflicts with console parsing
        /// </summary>
        private static Regex validateNameRe = new Regex(@"^[a-z_+-][a-z0-9_+.-]*$");

        /// <summary>
        /// Register a configuration variable in the global system
        /// Validates the variable name and prevents duplicate registration
        /// Called automatically during attribute discovery or manually for dynamic variables
        /// </summary>
        /// <param name="cvar">The ConfigVar instance to register</param>
        public static void RegisterConfigVar(ConfigVar cvar)
        {
            // Prevent duplicate registration
            if (ConfigVars.ContainsKey(cvar.name))
            {
                Debug.LogError("Trying to register cvar " + cvar.name + " twice");
                return;
            }

            // Validate name format to ensure console compatibility
            if (!validateNameRe.IsMatch(cvar.name))
            {
                Debug.LogError("Trying to register cvar with invalid name: " + cvar.name);
                return;
            }

            ConfigVars.Add(cvar.name, cvar);
        }

        #endregion


        #region Constructor and Properties
        
        /// <summary>
        /// Create a new configuration variable with specified properties
        /// Typically called automatically during attribute discovery
        /// For manual creation of dynamic variables at runtime
        /// </summary>
        /// <param name="name">Unique identifier following naming conventions</param>
        /// <param name="description">Human-readable description for help/documentation</param>
        /// <param name="defaultValue">Default value as string (will be converted as needed)</param>
        /// <param name="flags">Behavioral flags controlling save/replication/cheat status</param>
        public ConfigVar(string name, string description, string defaultValue, ConfigFlags flags = ConfigFlags.None)
        {
            this.name = name;
            this.flags = flags;
            this.description = description;
            this.defaultValue = defaultValue;
        }

        /// <summary>
        /// Get or set the current value of this configuration variable as a string
        /// 
        /// Setting Process:
        /// 1. Compares with current value to avoid unnecessary work
        /// 2. Updates dirty flags if value actually changes
        /// 3. Converts and caches integer and float representations
        /// 4. Marks variable as changed for ChangeCheck() detection
        /// 
        /// All type conversions happen automatically when this property is set
        /// Invalid conversions result in 0 values for IntValue/FloatValue
        /// </summary>
        public virtual string Value
        {
            get { return _stringValue; }
            set
            {
                // Skip update if value hasn't actually changed
                if (_stringValue == value)
                    return;
                    
                // Update dirty flags to indicate this type of variable has changed
                DirtyFlags |= flags;
                _stringValue = value;
                
                // Convert and cache integer representation (0 if conversion fails)
                if (!int.TryParse(value, out _intValue))
                    _intValue = 0;
                    
                // Convert and cache float representation (0 if conversion fails)
                // Uses invariant culture to ensure consistent parsing regardless of locale
                if (!float.TryParse(value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out _floatValue))
                    _floatValue = 0;
                    
                // Mark as changed for ChangeCheck() detection
                changed = true;
            }
        }

        /// <summary>
        /// Get the current value as an integer
        /// Returns 0 if the string value cannot be parsed as an integer
        /// Cached value updated automatically when Value property is set
        /// </summary>
        public int IntValue
        {
            get { return _intValue; }
        }

        /// <summary>
        /// Get the current value as a floating-point number
        /// Returns 0 if the string value cannot be parsed as a float
        /// Uses invariant culture for consistent parsing across different locales
        /// Cached value updated automatically when Value property is set
        /// </summary>
        public float FloatValue
        {
            get { return _floatValue; }
        }

        #endregion

        #region Private Implementation Methods
        
        /// <summary>
        /// Scan all loaded assemblies for fields marked with ConfigVarAttribute
        /// Creates ConfigVar instances and registers them in the global system
        /// Sets the static field values so code can reference them directly
        /// 
        /// Process:
        /// 1. Iterate through all loaded assemblies and their types
        /// 2. Find static fields with ConfigVarAttribute
        /// 3. Extract attribute parameters (name, description, default, flags)
        /// 4. Create ConfigVar instance with default value
        /// 5. Register in global dictionary
        /// 6. Set the static field reference
        /// 
        /// Performance: Uses reflection - should only be called once at startup
        /// Error Handling: Logs errors for invalid configurations but continues processing
        /// </summary>
        static void InjectAttributeConfigVars()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var _class in assembly.GetTypes())
                {
                    if (!_class.IsClass)
                        continue;
                        
                    foreach (var field in _class.GetFields(System.Reflection.BindingFlags.Instance |
                                                           System.Reflection.BindingFlags.Static |
                                                           System.Reflection.BindingFlags.NonPublic |
                                                           System.Reflection.BindingFlags.Public))
                    {
                        // Skip fields without ConfigVarAttribute
                        if (!field.IsDefined(typeof(ConfigVarAttribute), false))
                            continue;
                            
                        // ConfigVar fields must be static for global access
                        if (!field.IsStatic)
                        {
                            Debug.LogError("Cannot use ConfigVar attribute on non-static fields");
                            continue;
                        }

                        // Field must be of type ConfigVar
                        if (field.FieldType != typeof(ConfigVar))
                        {
                            Debug.LogError("Cannot use ConfigVar attribute on fields not of type ConfigVar");
                            continue;
                        }

                        // Extract attribute information
                        var attr = field.GetCustomAttributes(typeof(ConfigVarAttribute),
                            false)[0] as ConfigVarAttribute;
                            
                        // Generate name from attribute or use class.field format
                        var name = attr.Name != null ? attr.Name : _class.Name.ToLower() + "." + field.Name.ToLower();
                        
                        // Ensure field is not pre-initialized (should be null)
                        var cvar = field.GetValue(null) as ConfigVar;
                        if (cvar != null)
                        {
                            Debug.LogError("ConfigVars (" + name +
                                           ") should not be initialized from code; just marked with attribute");
                            continue;
                        }

                        // Create new ConfigVar instance with attribute parameters
                        cvar = new ConfigVar(name, attr.Description, attr.DefaultValue, attr.Flags);
                        cvar.ResetToDefault();
                        RegisterConfigVar(cvar);
                        
                        // Set the static field so code can reference it
                        field.SetValue(null, cvar);
                    }
                }
            }

            // Clear dirty flags as default values shouldn't count as modifications
            // Variables are only considered "dirty" when explicitly changed after initialization
            DirtyFlags = ConfigFlags.None;
        }

        /// <summary>
        /// Reset this variable back to its default value
        /// Used during system initialization and when user requests reset to defaults
        /// Will trigger change notifications if current value differs from default
        /// </summary>
        void ResetToDefault()
        {
            this.Value = defaultValue;
        }

        #endregion

        #region Change Detection
        
        /// <summary>
        /// Check if this variable has been modified since the last call to ChangeCheck()
        /// Useful for systems that need to react only when values actually change
        /// 
        /// Usage Pattern:
        /// if (myConfigVar.ChangeCheck())
        /// {
        ///     // Value was modified - update dependent systems
        ///     ApplyNewSettings();
        /// }
        /// 
        /// Returns: True if variable was modified, false otherwise
        /// Side Effect: Resets the changed flag to false after checking
        /// </summary>
        public bool ChangeCheck()
        {
            if (!changed)
                return false;
                
            changed = false;
            return true;
        }

        #endregion
    }
}
