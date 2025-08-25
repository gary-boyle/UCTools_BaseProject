// using UCTools_ConfigVariables;
//
// namespace GameFramework.ConfigVariables
// {
//     /// <summary>
//     /// Default configuration variables for the game framework
//     /// These are automatically registered and available through the ConfigService
//     /// </summary>
//     public static class DefaultConfigVariables
//     {
//         // Audio configuration
//         [ConfigVar(Name = "audio.enabled", DefaultValue = "1", Description = "Master control to enable or disable all audio", Flags = ConfigFlags.Save)]
//         public static ConfigVar AudioEnabled;
//         
//         [ConfigVar(Name = "audio.master_volume", DefaultValue = "1.0", Description = "Master volume level (0.0 - 1.0)", Flags = ConfigFlags.Save)]
//         public static ConfigVar MasterVolume;
//         
//         [ConfigVar(Name = "audio.music_volume", DefaultValue = "0.8", Description = "Music volume level (0.0 - 1.0)", Flags = ConfigFlags.Save)]
//         public static ConfigVar MusicVolume;
//         
//         [ConfigVar(Name = "audio.sfx_volume", DefaultValue = "1.0", Description = "SFX volume level (0.0 - 1.0)", Flags = ConfigFlags.Save)]
//         public static ConfigVar SfxVolume;
//         
//         // Graphics configuration
//         [ConfigVar(Name = "graphics.fullscreen", DefaultValue = "1", Description = "Fullscreen mode (0 = windowed, 1 = fullscreen)", Flags = ConfigFlags.Save)]
//         public static ConfigVar Fullscreen;
//         
//         [ConfigVar(Name = "graphics.resolution_width", DefaultValue = "1920", Description = "Screen resolution width", Flags = ConfigFlags.Save)]
//         public static ConfigVar ResolutionWidth;
//         
//         [ConfigVar(Name = "graphics.resolution_height", DefaultValue = "1080", Description = "Screen resolution height", Flags = ConfigFlags.Save)]
//         public static ConfigVar ResolutionHeight;
//         
//         [ConfigVar(Name = "graphics.quality_level", DefaultValue = "2", Description = "Graphics quality level (0-5)", Flags = ConfigFlags.Save)]
//         public static ConfigVar QualityLevel;
//         
//         [ConfigVar(Name = "graphics.vsync", DefaultValue = "1", Description = "Vertical sync (0 = disabled, 1 = enabled)", Flags = ConfigFlags.Save)]
//         public static ConfigVar VSync;
//         
//         // Gameplay configuration
//         [ConfigVar(Name = "game.difficulty", DefaultValue = "1", Description = "Game difficulty (0 = easy, 1 = normal, 2 = hard)", Flags = ConfigFlags.Save)]
//         public static ConfigVar Difficulty;
//         
//         [ConfigVar(Name = "game.auto_save", DefaultValue = "1", Description = "Auto-save enabled (0 = disabled, 1 = enabled)", Flags = ConfigFlags.Save)]
//         public static ConfigVar AutoSave;
//         
//         [ConfigVar(Name = "game.auto_save_interval", DefaultValue = "300", Description = "Auto-save interval in seconds", Flags = ConfigFlags.Save)]
//         public static ConfigVar AutoSaveInterval;
//         
//         // Input configuration
//         [ConfigVar(Name = "input.mouse_sensitivity", DefaultValue = "1.0", Description = "Mouse sensitivity multiplier", Flags = ConfigFlags.Save)]
//         public static ConfigVar MouseSensitivity;
//         
//         [ConfigVar(Name = "input.invert_y_axis", DefaultValue = "0", Description = "Invert Y axis (0 = normal, 1 = inverted)", Flags = ConfigFlags.Save)]
//         public static ConfigVar InvertYAxis;
//         
//         // Debug configuration
//         [ConfigVar(Name = "debug.show_fps", DefaultValue = "0", Description = "Show FPS counter (0 = hidden, 1 = visible)", Flags = ConfigFlags.Save)]
//         public static ConfigVar ShowFPS;
//         
//         [ConfigVar(Name = "debug.verbose_logging", DefaultValue = "0", Description = "Enable verbose logging (0 = disabled, 1 = enabled)", Flags = ConfigFlags.Save)]
//         public static ConfigVar VerboseLogging;
//         
//         [ConfigVar(Name = "debug.console_enabled", DefaultValue = "1", Description = "Enable debug console (0 = disabled, 1 = enabled)", Flags = ConfigFlags.Save)]
//         public static ConfigVar ConsoleEnabled;
//     }
//
// }