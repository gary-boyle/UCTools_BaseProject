using System;
using System.Linq;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using GameFramework.Config;
using GameFramework.ConsoleTool;
using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Interfaces;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Console command for controlling graphics settings
    /// Uses the clean event-driven architecture via GraphicsSettings_SO
    /// 
    /// Usage Examples:
    /// > graphics                 - Show current graphics status
    /// > graphics vsync on/off    - Enable/disable VSync
    /// > graphics fullscreen on   - Enable fullscreen
    /// > graphics resolution 2    - Set resolution by index
    /// > graphics quality 1       - Set quality by index
    /// </summary>
    public class GraphicsCommand : ConsoleCommandBase
    {
        #region Command Properties
        
        public override string CommandName => "graphics";
        public override string Description => "Control graphics settings (vsync, resolution, quality, fullscreen)";
        public override CategoryEnum Category => CategoryEnum.Graphics;
        public override int Tag => 201;
        
        #endregion

        #region Services
        
        private IConfigService _configService;
        private GraphicsSettings_SO _graphicsSettings;
        
        #endregion

        #region Command Execution
        
        public override void Execute(string[] args, IConsoleContext context)
        {
            if (!TryGetServices(context))
                return;

            if (args.Length == 0)
            {
                ShowGraphicsStatus(context);
                return;
            }

            string command = args[0].ToLowerInvariant();
            
            switch (command)
            {
                case "vsync":
                    HandleVSyncCommand(args, context);
                    break;
                    
                case "fullscreen":
                case "fs":
                    HandleFullscreenCommand(args, context);
                    break;
                    
                case "resolution":
                case "res":
                    HandleResolutionCommand(args, context);
                    break;
                    
                case "quality":
                    HandleQualityCommand(args, context);
                    break;
                    
                case "status":
                    ShowGraphicsStatus(context);
                    break;
                    
                default:
                    context.WriteError($"Unknown command: '{args[0]}'");
                    context.WriteLine(GetUsage());
                    break;
            }
        }
        
        #endregion

        #region Helper Methods
        
        private bool TryGetServices(IConsoleContext context)
        {
            try
            {
                _configService = GameManager.GetService<IConfigService>();
                
                if (_configService == null)
                {
                    context.WriteError("ConfigService not available");
                    return false;
                }

                _graphicsSettings = _configService.GetConfigCategory<GraphicsSettings_SO>();
                if (_graphicsSettings == null)
                {
                    context.WriteError("GraphicsSettings not found in ConfigService");
                    return false;
                }
                
                return true;
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to get services: {e.Message}");
                return false;
            }
        }
        
        private void HandleVSyncCommand(string[] args, IConsoleContext context)
        {
            if (args.Length != 2)
            {
                context.WriteError("VSync command requires on/off. Usage: graphics vsync <on|off>");
                return;
            }

            bool enabled = ParseBooleanArg(args[1]);

            try
            {
                _graphicsSettings.SetVSync(enabled);
                context.WriteLine($"VSync {(enabled ? "enabled" : "disabled")}");
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set VSync: {e.Message}");
            }
        }
        
        private void HandleFullscreenCommand(string[] args, IConsoleContext context)
        {
            if (args.Length != 2)
            {
                context.WriteError("Fullscreen command requires on/off. Usage: graphics fullscreen <on|off>");
                return;
            }

            bool? enabled = ParseBooleanArg(args[1]);
            if (enabled == null)
            {
                context.WriteError($"Invalid fullscreen value: '{args[1]}'. Use: on, off, true, false, 1, or 0");
                return;
            }

            try
            {
                _graphicsSettings.SetFullscreen(enabled.Value);
                context.WriteLine($"Fullscreen {(enabled.Value ? "enabled" : "disabled")}");
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set fullscreen: {e.Message}");
            }
        }
        
        private void HandleResolutionCommand(string[] args, IConsoleContext context)
        {
            if (args.Length == 1)
            {
                ShowResolutionOptions(context);
                return;
            }

            if (args.Length != 2)
            {
                context.WriteError("Resolution command requires index. Usage: graphics resolution <index>");
                return;
            }

            if (!TryParseInt(args[1], out int index, context, "resolution index"))
                return;

            try
            {
                var choices = _graphicsSettings.GetResolutionChoices();
                if (index < 0 || index >= choices.Length)
                {
                    context.WriteError($"Resolution index out of range. Valid range: 0-{choices.Length - 1}");
                    ShowResolutionOptions(context);
                    return;
                }

                _graphicsSettings.SetResolutionFromIndex(index);
                context.WriteLine($"Resolution set to: {choices[index]}");
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set resolution: {e.Message}");
            }
        }
        
        private void HandleQualityCommand(string[] args, IConsoleContext context)
        {
            if (args.Length == 1)
            {
                ShowQualityOptions(context);
                return;
            }

            if (args.Length != 2)
            {
                context.WriteError("Quality command requires index. Usage: graphics quality <index>");
                return;
            }

            if (!TryParseInt(args[1], out int index, context, "quality index"))
                return;

            try
            {
                var choices = _graphicsSettings.GetQualityChoices();
                if (index < 0 || index >= choices.Length)
                {
                    context.WriteError($"Quality index out of range. Valid range: 0-{choices.Length - 1}");
                    ShowQualityOptions(context);
                    return;
                }

                _graphicsSettings.SetQualityFromIndex(index);
                context.WriteLine($"Quality set to: {choices[index]}");
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set quality: {e.Message}");
            }
        }
        
        private bool ParseBooleanArg(string arg)
        {
            return arg.ToLowerInvariant() switch
            {
                "on" or "enable" or "true" or "1" => true,
                "off" or "disable" or "false" or "0" => false,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        private void ShowGraphicsStatus(IConsoleContext context)
        {
            try
            {
                context.WriteLine("Graphics Status:");
                context.WriteLine($"  Fullscreen: {(_graphicsSettings.fullscreen.Value ? "Yes" : "No")}");
                context.WriteLine($"  VSync: {(_graphicsSettings.vsync.Value ? "Enabled" : "Disabled")}");
                
                var resChoices = _graphicsSettings.GetResolutionChoices();
                var currentResIndex = _graphicsSettings.GetResolutionIndex();
                context.WriteLine($"  Resolution: {resChoices[currentResIndex]} (Index: {currentResIndex})");
                
                var qualityChoices = _graphicsSettings.GetQualityChoices();
                var currentQualityIndex = _graphicsSettings.GetQualityIndex();
                context.WriteLine($"  Quality: {qualityChoices[currentQualityIndex]} (Index: {currentQualityIndex})");
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to get graphics status: {e.Message}");
            }
        }
        
        private void ShowResolutionOptions(IConsoleContext context)
        {
            try
            {
                var choices = _graphicsSettings.GetResolutionChoices();
                var currentIndex = _graphicsSettings.GetResolutionIndex();
                
                context.WriteLine("Available resolutions:");
                for (int i = 0; i < choices.Length; i++)
                {
                    string marker = i == currentIndex ? " *" : "  ";
                    context.WriteLine($"{marker}[{i}] {choices[i]}");
                }
                context.WriteLine($"\nCurrent: {choices[currentIndex]} (Index: {currentIndex})");
                context.WriteLine("Usage: graphics resolution <index>");
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to show resolution options: {e.Message}");
            }
        }
        
        private void ShowQualityOptions(IConsoleContext context)
        {
            try
            {
                var choices = _graphicsSettings.GetQualityChoices();
                var currentIndex = _graphicsSettings.GetQualityIndex();
                
                context.WriteLine("Available quality levels:");
                for (int i = 0; i < choices.Length; i++)
                {
                    string marker = i == currentIndex ? " *" : "  ";
                    context.WriteLine($"{marker}[{i}] {choices[i]}");
                }
                context.WriteLine($"\nCurrent: {choices[currentIndex]} (Index: {currentIndex})");
                context.WriteLine("Usage: graphics quality <index>");
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to show quality options: {e.Message}");
            }
        }
        
        private async void SaveConfigAsync(IConsoleContext context)
        {
            try
            {
                await _configService.SaveConfigAsync();
            }
            catch (Exception e)
            {
                context.WriteWarning($"Settings updated but failed to save: {e.Message}");
            }
        }
        
        #endregion

        #region Documentation
        
        public override string GetUsage()
        {
            return "Usage: graphics [command] [value]\n" +
                   "  graphics                     - Show current graphics status\n" +
                   "  graphics vsync <on|off>      - Enable/disable VSync\n" +
                   "  graphics fullscreen <on|off> - Enable/disable fullscreen\n" +
                   "  graphics resolution [index]  - Show options or set resolution\n" +
                   "  graphics quality [index]     - Show options or set quality\n" +
                   "  graphics status              - Show current graphics status\n" +
                   "\n" +
                   "Examples:\n" +
                   "  graphics vsync on            - Enable VSync\n" +
                   "  graphics resolution 2        - Set resolution to index 2\n" +
                   "  graphics resolution          - List available resolutions\n" +
                   "\n" +
                   "Note: Changes are automatically applied and saved.\n" +
                   "      Use commands without values to see available options.";
        }
        
        public override bool ValidateArgs(string[] args)
        {
            return args.Length <= 2;
        }
        
        #endregion
    }
}
