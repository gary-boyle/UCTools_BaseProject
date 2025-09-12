using System;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using GameFramework.Config.ScriptableObjects;
using GameFramework.UI.Popups;
using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Interfaces;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Console command for controlling the DebugPopup visibility via debug settings
    /// Uses the clean event-driven architecture - updates settings, services handle the rest
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
        
        private IUIService _uiService;
        private DebugSettings_SO _debugSettings;
        
        #endregion

        #region Command Execution
        
        public override void Execute(string[] args, IConsoleContext context)
        {
            // Get services and settings
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
        
        /// <summary>
        /// Get required services and validate they're available
        /// </summary>
        private bool TryGetServices(IConsoleContext context)
        {
            try
            {
                _uiService = GameManager.GetService<IUIService>();
                
                if (_uiService == null)
                {
                    context.WriteError("UIService not available");
                    return false;
                }

                // Get the debug settings ScriptableObject
                _debugSettings = SettingsRegistry.Get<DebugSettings_SO>();
                if (_debugSettings == null)
                {
                    context.WriteError("DebugSettings not found in ConfigService");
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
        
        /// <summary>
        /// Toggle debug popup state - much simpler now!
        /// </summary>
        private void ToggleDebugPopup(IConsoleContext context)
        {
            try
            {
                // Get current state from settings
                bool currentState = _debugSettings.ShowDebugInfo.Value;
                bool newState = !currentState;
        
                // Update settings - UIService will handle the popup automatically
                _debugSettings.SetShowDebugInfo(newState);
                
                context.WriteLine($"Debug popup {(newState ? "enabled" : "disabled")}");
                
                // Save config to persist changes
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to toggle debug popup: {e.Message}");
            }
        }
        
        /// <summary>
        /// Set debug popup to specific state
        /// </summary>
        private void SetDebugPopup(bool enabled, IConsoleContext context)
        {
            try
            {
                // Update settings - UIService handles the rest via events
                _debugSettings.SetShowDebugInfo(enabled);
                
                context.WriteLine($"Debug popup {(enabled ? "enabled" : "disabled")}");
                
                // Save config to persist changes
                SaveConfigAsync(context);
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to set debug popup state: {e.Message}");
            }
        }
        
        /// <summary>
        /// Show current debug popup status
        /// </summary>
        private void ShowDebugPopupStatus(IConsoleContext context)
        {
            try
            {
                bool configEnabled = _debugSettings.ShowDebugInfo.Value;
                bool popupVisible = _uiService.IsPopupOpen<DebugPopup>();
                
                context.WriteLine("Debug Popup Status:");
                context.WriteLine($"  Config Setting: {(configEnabled ? "Enabled" : "Disabled")}");
                context.WriteLine($"  Currently Visible: {(popupVisible ? "Yes" : "No")}");
                
                // Show additional UI state info if popup is visible
                if (popupVisible)
                {
                    bool isCurrent = _uiService.IsCurrentPopup<DebugPopup>();
                    context.WriteLine($"  Position: {(isCurrent ? "Current (top)" : "In stack")}");
                    
                    if (!isCurrent)
                    {
                        int stackPosition = _uiService.GetPopupStackPosition<DebugPopup>();
                        context.WriteLine($"  Stack Position: {stackPosition}");
                    }
                }
                
                // Show state consistency
                if (configEnabled != popupVisible)
                {
                    context.WriteWarning("  Note: Config and UI state are inconsistent - this may resolve automatically");
                }
            }
            catch (Exception e)
            {
                context.WriteError($"Failed to get debug popup status: {e.Message}");
            }
        }
        
        /// <summary>
        /// Save config asynchronously with error handling
        /// </summary>
        private async void SaveConfigAsync(IConsoleContext context)
        {
            try
            {
                await SettingsRegistry.SaveAllSettingsAsync();
            }
            catch (Exception e)
            {
                context.WriteWarning($"Settings updated but failed to save to disk: {e.Message}");
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
                   "Note: Changes are automatically saved to configuration.\n" +
                   "      The UIService handles popup display automatically.";
        }
        
        public override bool ValidateArgs(string[] args)
        {
            return args.Length <= 1;
        }
        
        #endregion
    }
}
