using System;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using GameFramework.Config.ScriptableObjects;
using GameFramework.Input.Interfaces;
using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Interfaces;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Console command for controlling input settings
    /// Uses the clean event-driven architecture via InputSettings_SO
    /// 
    /// Usage Examples:
    /// > input                        - Show current input status
    /// > input sensitivity 150        - Set mouse sensitivity to 150%
    /// > input invert on/off          - Enable/disable Y-axis inversion
    /// > input reset                  - Reset all input settings to defaults
    /// </summary>
    public class InputCommand : ConsoleCommandBase
    {
        #region Command Properties
        
        public override string CommandName => "input";
        public override string Description => "Control input settings (mouse sensitivity, Y-axis inversion)";
        public override CategoryEnum Category => CategoryEnum.System;
        public override int Tag => 202;
        
        #endregion

        #region Services
        
        private IInputManager _inputManager;
        private InputSettings_SO _inputSettings;
        
        #endregion

        #region Command Execution
        
        public override void Execute(string[] args, IConsoleContext context)
        {
            if (!TryGetServices(context))
                return;

            if (args.Length == 0)
            {
                ShowInputStatus(context);
                return;
            }

            string command = args[0].ToLowerInvariant();
            
            switch (command)
            {
                case "sensitivity":
                case "sens":
                    HandleSensitivityCommand(args, context);
                    break;
                    
                case "invert":
                case "invertyaxis":
                case "inverty":
                    HandleInvertCommand(args, context);
                    break;
                    
                case "reset":
                    ResetInputSettings(context);
                    break;
                    
                case "status":
                    ShowInputStatus(context);
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
                _inputManager = GameManager.GetService<IInputManager>();

                if (_inputManager == null)
                {
                    context.WriteError("InputManager not available");
                    return false;
                }

                _inputSettings = SettingsRegistry.Get<InputSettings_SO>();
                if (_inputSettings == null)
                {
                    context.WriteError("InputSettings not found in ConfigService");
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
        
        private void HandleSensitivityCommand(string[] args, IConsoleContext context)
        {
            if (args.Length != 2)
            {
                context.WriteError("Sensitivity command requires a value. Usage: input sensitivity <10-1000>");
                return;
            }

            if (!TryParseInt(args[1], out int sensitivityPercent, context, "sensitivity"))
                return;

            if (sensitivityPercent < 10 || sensitivityPercent > 1000)
            {
                context.WriteError("Sensitivity must be between 10% and 1000%");
                return;
            }

            try
            {
                _inputSettings.SetMouseSensitivityFromPercentage(sensitivityPercent);
                context.WriteLine($"Mouse sensitivity set to {sensitivityPercent}%");
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set mouse sensitivity: {e.Message}");
            }
        }
        
        private void HandleInvertCommand(string[] args, IConsoleContext context)
        {
            if (args.Length != 2)
            {
                context.WriteError("Invert command requires on/off. Usage: input invert <on|off>");
                return;
            }

            bool? enabled = ParseBooleanArg(args[1]);
            if (enabled == null)
            {
                context.WriteError($"Invalid invert value: '{args[1]}'. Use: on, off, true, false, 1, or 0");
                return;
            }

            try
            {
                _inputSettings.SetInvertYAxis(enabled.Value);
                context.WriteLine($"Y-axis inversion {(enabled.Value ? "enabled" : "disabled")}");
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set Y-axis inversion: {e.Message}");
            }
        }
        
        private void ResetInputSettings(IConsoleContext context)
        {
            try
            {
                _inputSettings.ResetMouseSettings();
                context.WriteLine("Input settings reset to defaults");
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to reset input settings: {e.Message}");
            }
        }
        
        private bool? ParseBooleanArg(string arg)
        {
            return arg.ToLowerInvariant() switch
            {
                "on" or "enable" or "true" or "1" => true,
                "off" or "disable" or "false" or "0" => false,
                _ => null
            };
        }
        
        private void ShowInputStatus(IConsoleContext context)
        {
            try
            {
                float sensitivity = _inputSettings.GetMouseSensitivity();
                bool invertY = _inputSettings.GetInvertYAxis();
                
                // Get current applied settings from InputManager for verification
                float appliedSensitivity = _inputManager.GetMouseSensitivity();
                bool appliedInvertY = _inputManager.GetInvertYAxis();
                
                context.WriteLine("Input Settings Status:");
                context.WriteLine($"  Mouse Sensitivity: {_inputSettings.GetMouseSensitivityAsPercentage()}% ({sensitivity:F2})");
                context.WriteLine($"  Y-Axis Inversion: {(invertY ? "Enabled" : "Disabled")}");
                
                // Show if there's any discrepancy between config and applied settings
                if (Math.Abs(sensitivity - appliedSensitivity) > 0.001f || invertY != appliedInvertY)
                {
                    context.WriteWarning("  Note: Applied settings differ from config (this may resolve automatically)");
                    context.WriteLine($"  Applied Sensitivity: {appliedSensitivity:F2}");
                    context.WriteLine($"  Applied Y-Inversion: {(appliedInvertY ? "Enabled" : "Disabled")}");
                }
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to get input status: {e.Message}");
            }
        }
        
        private async void SaveConfigAsync(IConsoleContext context)
        {
            try
            {
                await SettingsRegistry.SaveAllSettingsAsync();
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
            return "Usage: input [command] [value]\n" +
                   "  input                        - Show current input status\n" +
                   "  input sensitivity <10-1000>  - Set mouse sensitivity percentage\n" +
                   "  input invert <on|off>        - Enable/disable Y-axis inversion\n" +
                   "  input reset                  - Reset all input settings to defaults\n" +
                   "  input status                 - Show current input status\n" +
                   "\n" +
                   "Examples:\n" +
                   "  input sensitivity 150        - Set mouse sensitivity to 150%\n" +
                   "  input invert on              - Enable Y-axis inversion\n" +
                   "\n" +
                   "Note: Changes are automatically applied and saved.\n" +
                   "      Sensitivity range: 10% (0.1x) to 1000% (10x)";
        }
        
        public override bool ValidateArgs(string[] args)
        {
            return args.Length <= 2;
        }
        
        #endregion
    }
}
