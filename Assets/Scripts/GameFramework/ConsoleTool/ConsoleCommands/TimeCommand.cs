using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Interfaces;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using GameFramework.Services.Data;
using GameFramework.Utilities;
using UnityEngine;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Console command for querying and manipulating game time
    /// Integrates with TimeService and GameDataService to provide time management functionality
    /// 
    /// Design:
    /// - Uses TimeService for reading current time and time formatting
    /// - Accesses GameDataService directly to modify GameSessionData.GameTime
    /// - Supports multiple time input formats for user convenience
    /// - Provides comprehensive validation and error handling
    /// - Includes -h flag for displaying help/usage information
    /// 
    /// Usage Examples:
    /// > time                    - Display current game time
    /// > time -h                 - Display help/usage information
    /// > time 3600               - Set game time to 3600 seconds (1 hour)
    /// > time 1:30:45            - Set game time to 1 hour, 30 minutes, 45 seconds
    /// > time reset              - Reset game time to 0
    /// > time add 300            - Add 300 seconds to current game time
    /// > time subtract 120       - Subtract 120 seconds from current game time
    /// 
    /// Pros:
    /// - Flexible time input formats (seconds, HH:MM:SS)
    /// - Comprehensive error handling and validation
    /// - Integrates seamlessly with existing TimeService
    /// - Supports both absolute and relative time operations
    /// - Provides immediate feedback on time changes
    /// - Standard -h flag for help display
    /// 
    /// Cons:
    /// - Requires direct access to GameDataService for time modification
    /// - Time changes bypass TimeService's normal tracking mechanisms
    /// - Could potentially cause issues if game logic depends on time continuity
    /// </summary>
    public class TimeCommand : ConsoleCommandBase
    {
        #region Command Properties
        
        /// <summary>
        /// Command name - what users type to execute this command
        /// </summary>
        public override string CommandName => "time";
        
        /// <summary>
        /// Brief description for help system
        /// </summary>
        public override string Description => "Query and manipulate game time (use -h for detailed help)";
        
        /// <summary>
        /// Categorize under System commands for organization
        /// </summary>
        public override CategoryEnum Category => CategoryEnum.System;
        
        /// <summary>
        /// Tag for bulk operations - using system-level tag
        /// </summary>
        public override int Tag => 100;
        
        #endregion

        #region Services
        
        private ITimeService _timeService;
        private IGameDataService _gameDataService;
        
        /// <summary>
        /// Lazy initialization of services to avoid circular dependencies
        /// Services are retrieved when first needed
        /// </summary>
        private void EnsureServicesInitialized()
        {
            if (_timeService == null)
            {
                _timeService = GameManager.GetService<ITimeService>();
            }
            
            if (_gameDataService == null)
            {
                _gameDataService = GameManager.GetService<IGameDataService>();
            }
        }
        
        #endregion

        #region Command Execution
        
        /// <summary>
        /// Main command execution method
        /// Handles different argument patterns for time operations
        /// Checks for -h flag first to display help
        /// </summary>
        /// <param name="args">Command arguments</param>
        /// <param name="context">Console context for output</param>
        public override void Execute(string[] args, IConsoleContext context)
        {
            // Check for help flag first - takes precedence over all other arguments
            if (ContainsHelpFlag(args))
            {
                DisplayDetailedHelp(context);
                return;
            }
            
            // Ensure services are available
            EnsureServicesInitialized();
            
            if (_timeService == null)
            {
                context.WriteError("TimeService is not available. Ensure the service is properly initialized.");
                return;
            }
            
            if (_gameDataService == null)
            {
                context.WriteError("GameDataService is not available. Ensure the service is properly initialized.");
                return;
            }

            // Handle different argument patterns
            switch (args.Length)
            {
                case 0:
                    // No arguments - display current time
                    DisplayCurrentTime(context);
                    break;
                    
                case 1:
                    // One argument - could be time value, "reset", or invalid
                    HandleSingleArgument(args[0], context);
                    break;
                    
                case 2:
                    // Two arguments - operation with value (add/subtract)
                    HandleTwoArguments(args[0], args[1], context);
                    break;
                    
                default:
                    // Invalid argument count
                    context.WriteError("Too many arguments provided.");
                    context.WriteLine("Use 'time -h' for detailed usage information.");
                    break;
            }
        }
        
        #endregion

        #region Help System
        
        /// <summary>
        /// Check if any argument contains the help flag
        /// </summary>
        private bool ContainsHelpFlag(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.Equals("-h", System.StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--help", System.StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("help", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Display detailed help information with examples and formatting
        /// </summary>
        private void DisplayDetailedHelp(IConsoleContext context)
        {
            context.WriteLine("=== TIME COMMAND HELP ===");
            context.WriteLine("");
            context.WriteLine("DESCRIPTION:");
            context.WriteLine("  Query and manipulate game time. Integrates with TimeService and GameDataService");
            context.WriteLine("  to provide comprehensive time management for debugging and testing.");
            context.WriteLine("");
            
            context.WriteLine("BASIC USAGE:");
            context.WriteLine("  time                     - Display current game time and status");
            context.WriteLine("  time -h                  - Show this help information");
            context.WriteLine("");
            
            context.WriteLine("SET ABSOLUTE TIME:");
            context.WriteLine("  time 3600                - Set time to 3600 seconds (1 hour)");
            context.WriteLine("  time 1:30:45             - Set time to 1 hour, 30 minutes, 45 seconds");
            context.WriteLine("  time 90:30               - Set time to 90 minutes, 30 seconds");
            context.WriteLine("  time 45                  - Set time to 45 seconds");
            context.WriteLine("  time reset               - Reset time to 0 seconds");
            context.WriteLine("");
            
            context.WriteLine("MODIFY RELATIVE TIME:");
            context.WriteLine("  time add 300             - Add 300 seconds to current time");
            context.WriteLine("  time add 5:30            - Add 5 minutes, 30 seconds to current time");
            context.WriteLine("  time add 1:30:45         - Add 1 hour, 30 minutes, 45 seconds");
            context.WriteLine("  time subtract 120        - Subtract 120 seconds from current time");
            context.WriteLine("  time sub 2:15            - Subtract 2 minutes, 15 seconds (sub is shorthand)");
            context.WriteLine("");
            
            context.WriteLine("TIME FORMAT EXAMPLES:");
            context.WriteLine("  45          = 45 seconds");
            context.WriteLine("  5:30        = 5 minutes, 30 seconds (330 total seconds)");
            context.WriteLine("  1:30:45     = 1 hour, 30 minutes, 45 seconds (5445 total seconds)");
            context.WriteLine("  90:15       = 90 minutes, 15 seconds (5415 total seconds)");
            context.WriteLine("");
            
            context.WriteLine("EXAMPLES WITH OUTPUT:");
            // Show current time as example
            EnsureServicesInitialized();
            if (_timeService != null)
            {
                long currentTime = _timeService.GameTime;
                string formatted = _timeService.GetFormattedGameTime();
                context.WriteLine($"  Current time: {currentTime}s ({formatted})");
                context.WriteLine($"  > time add 300    → Would set time to {currentTime + 300}s");
                context.WriteLine($"  > time reset      → Would set time to 0s");
            }
            context.WriteLine("");
            
            context.WriteLine("NOTES:");
            context.WriteLine("  • Time cannot be set to negative values (minimum is 0)");
            context.WriteLine("  • Large time changes may affect game systems expecting time continuity");
            context.WriteLine("  • Changes are immediately saved to GameSessionData");
            context.WriteLine("  • Time modifications bypass normal TimeService tracking");
            context.WriteLine("  • Use 'time' without arguments to check current time and tracking status");
            context.WriteLine("");
            
            context.WriteLine("=== END HELP ===");
        }
        
        #endregion

        #region Argument Handlers
        
        /// <summary>
        /// Display current game time with formatted output
        /// </summary>
        private void DisplayCurrentTime(IConsoleContext context)
        {
            long currentTime = _timeService.GameTime;
            string formattedTime = _timeService.GetFormattedGameTime();
            
            context.WriteLine($"Current Game Time: {currentTime} seconds ({formattedTime})");
            context.WriteLine($"Tracking Status: {(_timeService.IsTrackingGameTime ? "Active" : "Paused/Inactive")}");
            context.WriteLine("Use 'time -h' for usage help.");
        }
        
        /// <summary>
        /// Handle single argument commands (set time or special operations)
        /// </summary>
        private void HandleSingleArgument(string arg, IConsoleContext context)
        {
            // Check for special commands first
            if (arg.Equals("reset", System.StringComparison.OrdinalIgnoreCase))
            {
                SetGameTime(0, context);
                return;
            }
            
            // Try to parse as time value
            if (TryParseTimeValue(arg, out long timeInSeconds, context))
            {
                SetGameTime(timeInSeconds, context);
            }
            else
            {
                context.WriteLine("Use 'time -h' for usage help.");
            }
        }
        
        /// <summary>
        /// Handle two-argument commands (add/subtract operations)
        /// </summary>
        private void HandleTwoArguments(string operation, string value, IConsoleContext context)
        {
            // Parse the time value
            if (!TryParseTimeValue(value, out long timeInSeconds, context))
            {
                context.WriteLine("Use 'time -h' for usage help.");
                return;
            }
            
            long currentTime = _timeService.GameTime;
            
            switch (operation.ToLower())
            {
                case "add":
                    SetGameTime(currentTime + timeInSeconds, context);
                    break;
                    
                case "subtract":
                case "sub":
                    long newTime = (long)Mathf.Max(0, currentTime - timeInSeconds);
                    SetGameTime(newTime, context);
                    if (newTime == 0 && currentTime - timeInSeconds < 0)
                    {
                        context.WriteLine("Note: Game time cannot be negative. Set to 0.");
                    }
                    break;
                    
                default:
                    context.WriteError($"Unknown operation: '{operation}'. Valid operations: add, subtract (sub)");
                    context.WriteLine("Use 'time -h' for usage help.");
                    break;
            }
        }
        
        #endregion

        #region Time Parsing and Setting
        
        /// <summary>
        /// Parse time value from string - supports seconds and HH:MM:SS format
        /// </summary>
        private bool TryParseTimeValue(string input, out long seconds, IConsoleContext context)
        {
            seconds = 0;
            
            // Try parsing as plain integer (seconds)
            if (long.TryParse(input, out seconds))
            {
                if (seconds < 0)
                {
                    context.WriteError("Time cannot be negative.");
                    return false;
                }
                return true;
            }
            
            // Try parsing as time format (HH:MM:SS, MM:SS, or SS)
            if (TryParseTimeFormat(input, out seconds))
            {
                return true;
            }
            
            context.WriteError($"Invalid time format: '{input}'");
            context.WriteLine("Valid formats: seconds (3600) or time format (1:30:45, 90:30, 45)");
            return false;
        }
        
        /// <summary>
        /// Parse time in HH:MM:SS format
        /// Supports formats: SS, MM:SS, HH:MM:SS
        /// </summary>
        private bool TryParseTimeFormat(string input, out long totalSeconds)
        {
            totalSeconds = 0;
            
            string[] parts = input.Split(':');
            
            try
            {
                switch (parts.Length)
                {
                    case 1: // SS
                        totalSeconds = long.Parse(parts[0]);
                        break;
                        
                    case 2: // MM:SS
                        totalSeconds = long.Parse(parts[0]) * 60 + long.Parse(parts[1]);
                        break;
                        
                    case 3: // HH:MM:SS
                        totalSeconds = long.Parse(parts[0]) * 3600 + 
                                     long.Parse(parts[1]) * 60 + 
                                     long.Parse(parts[2]);
                        break;
                        
                    default:
                        return false;
                }
                
                return totalSeconds >= 0;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Set the game time by directly modifying GameSessionData
        /// Provides feedback on the time change
        /// </summary>
        private void SetGameTime(long newTimeInSeconds, IConsoleContext context)
        {
            var gameSession = _gameDataService.GetGameSessionData();
            if (gameSession == null)
            {
                context.WriteError("Game session data is not available.");
                return;
            }
            
            long previousTime = gameSession.GameTime;
            gameSession.GameTime = newTimeInSeconds;
            
            string formattedNewTime = TimeUtilities.FormatTimeFromSeconds(newTimeInSeconds);
            string formattedPreviousTime = TimeUtilities.FormatTimeFromSeconds(previousTime);
            
            context.WriteLine($"Game time changed from {previousTime}s ({formattedPreviousTime}) to {newTimeInSeconds}s ({formattedNewTime})");
            
            // Warn about potential side effects
            if (Mathf.Abs(newTimeInSeconds - previousTime) > 60)
            {
                context.WriteLine("Warning: Large time change may affect game systems that depend on time continuity.");
            }
        }
        
        #endregion

        #region Documentation and Validation
        
        /// <summary>
        /// Provide brief usage summary - detailed help available via -h flag
        /// </summary>
        public override string GetUsage()
        {
            return "Usage: time [value|operation] [value] | time -h\n" +
                   "\n" +
                   "Quick examples:\n" +
                   "  time                     - Show current time\n" +
                   "  time -h                  - Show detailed help\n" +
                   "  time 3600                - Set time to 3600 seconds\n" +
                   "  time 1:30:45             - Set time to 1h 30m 45s\n" +
                   "  time add 300             - Add 5 minutes\n" +
                   "  time reset               - Reset to 0\n" +
                   "\n" +
                   "Use 'time -h' for comprehensive usage information.";
        }
        
        /// <summary>
        /// Validate argument count before execution
        /// Always returns true since -h flag can appear with any argument count
        /// </summary>
        public override bool ValidateArgs(string[] args)
        {
            // Always return true since -h flag takes precedence and can appear anywhere
            // Actual validation happens in Execute method after help flag check
            return true;
        }
        
        #endregion
    }
}
