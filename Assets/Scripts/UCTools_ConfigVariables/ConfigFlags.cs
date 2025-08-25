using System;

namespace UCTools_ConfigVariables
{
    /// <summary>
    /// Behavioral flags that control how configuration variables are handled
    /// Can be combined using bitwise OR to give variables multiple behaviors
    /// 
    /// Usage Examples:
    /// - Flags.Save: Variable persists between sessions
    /// - Flags.Save | Flags.ServerInfo: Variable persists AND is sent to clients
    /// - Flags.Cheat: Variable can only be modified when cheats are enabled
    /// </summary>
    [Flags]
    public enum ConfigFlags
    {
        /// <summary>Default behavior - temporary variable, not saved or replicated</summary>
        None = 0x0,
        
        /// <summary>
        /// Variable should be saved to configuration file and loaded on startup
        /// Used for user preferences, graphics settings, key bindings, etc.
        /// Triggers file I/O operations when modified
        /// </summary>
        Save = 0x1,
        
        /// <summary>
        /// Variable is considered a cheat and can only be modified when cheats are enabled
        /// Used for debug variables, god mode, infinite resources, etc.
        /// Provides protection against accidental or unauthorized modifications
        /// </summary>
        Cheat = 0x2,
        
        /// <summary>
        /// Variable is sent from server to clients when they connect or when value changes
        /// Used for server settings that affect gameplay: time limits, player counts, etc.
        /// Ensures all clients have consistent server configuration
        /// </summary>
        ServerInfo = 0x4,
        
        /// <summary>
        /// Variable is sent from client to server when connecting or when value changes
        /// Used for client preferences that server needs to know: preferred name, color, etc.
        /// Allows server to customize experience based on client preferences
        /// </summary>
        ClientInfo = 0x8,
        
        /// <summary>
        /// Variable was created dynamically by user (not predefined in code)
        /// Used for custom variables created through console or scripting
        /// May have different validation or cleanup behavior
        /// </summary>
        User = 0x10,
    }
}