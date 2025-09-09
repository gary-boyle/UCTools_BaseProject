using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using GameFramework.UI.Popups;
using GameFramework.ConsoleTool;
using GameFramework.ConsoleTool.Enums;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Console command for controlling the DebugPopup visibility
    /// 
    /// Usage Examples:
    /// > debug                    - Toggle debug popup on/off
    /// > debug on                 - Enable debug popup
    /// > debug off                - Disable debug popup
    /// > debug status             - Show current debug popup state
    /// </summary>
    public class DebugCommand : ConsoleCommandBase
    {
        #region Command Properties
        
        public override string CommandName => "debug";
        public override string Description => "Control debug popup visibility";
        public override CategoryEnum Category => CategoryEnum.Debug;
        public override int Tag => 100;
        
        #endregion

        #region Services
        
        private IConfigService _configService;
        private IUIService _uiService;
        
        #endregion

        #region Command Execution
        
        public override void Execute(string[] args, IConsoleContext context)
        {
            // Get services
            if (!TryGetServices(context))
                return;

            switch (args.Length)
            {
                case 0:
                    ToggleDebugPopup(context);
                    break;
                    
                case 1:
                    string command = args[0].ToLowerInvariant();
                    switch (command)
                    {
                        case "on":
                        case "enable":
                        case "true":
                        case "1":
                            SetDebugPopup(true, context);
                            break;
                            
                        case "off":
                        case "disable":
                        case "false":
                        case "0":
                            SetDebugPopup(false, context);
                            break;
                            
                        case "status":
                        case "state":
                            ShowDebugPopupStatus(context);
                            break;
                            
                        default:
                            context.WriteError($"Unknown argument: '{args[0]}'");
                            context.WriteLine(GetUsage());
                            break;
                    }
                    break;
                    
                default:
                    context.WriteError("Too many arguments.");
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
                _uiService = GameManager.GetService<IUIService>();
                
                if (_configService == null)
                {
                    context.WriteError("ConfigService not available");
                    return false;
                }
                
                if (_uiService == null)
                {
                    context.WriteError("UIService not available");
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
        
        private async void ToggleDebugPopup(IConsoleContext context)
        {
            try
            {
                // ✅ Check actual UI state, not just config
                bool isCurrentlyVisible = _uiService.IsPopupOpen<DebugPopup>();
                bool newState = !isCurrentlyVisible;
        
                await SetDebugPopupState(newState, context);
                context.WriteLine($"Debug popup {(newState ? "enabled" : "disabled")}");
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to toggle debug popup: {e.Message}");
            }
        }
        
        private async void SetDebugPopup(bool enabled, IConsoleContext context)
        {
            try
            {
                await SetDebugPopupState(enabled, context);
                context.WriteLine($"Debug popup {(enabled ? "enabled" : "disabled")}");
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set debug popup state: {e.Message}");
            }
        }
        
        private async Task SetDebugPopupState(bool enabled, IConsoleContext context)
        {
            // Update the config setting
            _configService.SetConfigValue("debug.show_debug_info", enabled);
            
            if (enabled)
            {
                if (!_uiService.IsCurrentPopup<DebugPopup>())
                {
                    await _uiService.ShowPopupAsync<DebugPopup>();
                }
            }
            else
            {
                await HideDebugPopupSafely();
            }
            
            // Save the config to persist the change
            await _configService.SaveConfigAsync();
        }
        
        private async Task HideDebugPopupSafely()
        {
            if (_uiService.IsCurrentPopup<DebugPopup>())
            {
                await _uiService.HidePopupAsync<DebugPopup>();
                return;
            }
            
            var debugPopup = _uiService.GetPopup<DebugPopup>();
            if (debugPopup != null && debugPopup.IsVisible)
            {
                debugPopup.Hide();
            }
        }
        
        private void ShowDebugPopupStatus(IConsoleContext context)
        {
            try
            {
                bool configEnabled = _configService.GetConfigValue<bool>("debug.show_debug_info");
                bool popupVisible = _uiService.IsPopupOpen<DebugPopup>();
                
                context.WriteLine($"Debug Popup Status:");
                context.WriteLine($"  Config Setting: {(configEnabled ? "Enabled" : "Disabled")}");
                context.WriteLine($"  Currently Visible: {(popupVisible ? "Yes" : "No")}");
                
                if (popupVisible)
                {
                    bool isCurrent = _uiService.IsCurrentPopup<DebugPopup>();
                    int stackPosition = _uiService.GetPopupStackPosition<DebugPopup>();
                    context.WriteLine($"  Stack Position: {(isCurrent ? "Current (top)" : $"Position {stackPosition}")}");
                }
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to get debug popup status: {e.Message}");
            }
        }
        
        #endregion

        #region Documentation
        
        public override string GetUsage()
        {
            return "Usage: debug [command]\n" +
                   "  debug              - Toggle debug popup on/off\n" +
                   "  debug on           - Enable debug popup\n" +
                   "  debug off          - Disable debug popup\n" +
                   "  debug status       - Show current debug popup state\n" +
                   "\n" +
                   "Aliases:\n" +
                   "  on:  enable, true, 1\n" +
                   "  off: disable, false, 0\n" +
                   "\n" +
                   "Note: Changes are automatically saved to configuration";
        }
        
        public override bool ValidateArgs(string[] args)
        {
            return args.Length <= 1;
        }
        
        #endregion
    }
}
