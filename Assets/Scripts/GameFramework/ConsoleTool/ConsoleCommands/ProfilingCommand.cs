using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Interfaces;
using GameFramework.Core;
using GameFramework.Services.Interfaces;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Events;
using UnityEngine;
using System.IO;
using GameFramework.EventSystem.Events;
using UnityEngine.InputSystem;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Console command for controlling frame-based profiling sessions and monitoring performance.
    /// 
    /// Design:
    /// - Simple command structure with clear subcommand handlers
    /// - Direct service interaction without static state
    /// - Clean separation between command parsing and execution
    /// 
    /// Pros:
    /// - Stateless design prevents threading issues
    /// - Clear command structure with consistent error handling
    /// - Direct service calls for real-time feedback
    /// 
    /// Cons:
    /// - Requires ProfilingService to be available
    /// - No offline command caching
    /// </summary>
    public class ProfilingCommand : ConsoleCommandBase
    {
        #region Properties
        
        public override string CommandName => "profiling";
        public override string Description => "Control profiling sessions and monitor performance";
        public override CategoryEnum Category => CategoryEnum.Debug;
        public override int Tag => 200;
        
        #endregion

        #region Command Execution
        
        public override void Execute(string[] args, IConsoleContext context)
        {
            var profilingService = GameManager.GetService<IProfilingService>();
            if (profilingService == null)
            {
                context.WriteError("ProfilingService not available");
                return;
            }
            
            if (args.Length == 0)
            {
                ShowStatus(profilingService, context);
                return;
            }
            
            string subCommand = args[0].ToLower();
            
            switch (subCommand)
            {
                case "status":
                    ShowStatus(profilingService, context);
                    break;
                    
                case "start":
                    HandleStartCommand(args, profilingService, context);
                    break;
                    
                case "stop":
                    HandleStopCommand(profilingService, context);
                    break;
                    
                case "current":
                    ShowCurrentMetrics(profilingService, context);
                    break;
                    
                case "clear":
                    HandleClearCommand(profilingService, context);
                    break;
                    
                case "config":
                    HandleConfigCommand(args, profilingService, context);
                    break;
                    
                case "path":
                    ShowFilePath(context);
                    break;
                    
                default:
                    context.WriteError($"Unknown subcommand: {subCommand}");
                    context.WriteLine(GetUsage());
                    break;
            }
        }
        
        #endregion

        #region Command Handlers
        
        /// <summary>
        /// Shows current profiling service status and metrics
        /// </summary>
        private void ShowStatus(IProfilingService profilingService, IConsoleContext context)
        {
            context.WriteLine("=== Profiling Service Status ===");
            context.WriteLine($"Service Initialized: {(profilingService.IsInitialized ? "✓" : "✗")}");
            context.WriteLine($"Session Active: {(profilingService.IsSessionActive ? "✓" : "✗")}");
            
            if (profilingService.IsSessionActive)
            {
                float progress = profilingService.SessionProgress;
                context.WriteLine($"Session Progress: {progress:P1}");
                
                // Simple progress bar
                int barLength = 20;
                int filledLength = Mathf.RoundToInt(progress * barLength);
                string progressBar = "[" + new string('=', filledLength) + new string('-', barLength - filledLength) + "]";
                context.WriteLine($"Progress: {progressBar}");
            }
            
            ShowCurrentMetrics(profilingService, context);
        }
        
        /// <summary>
        /// Displays current performance metrics
        /// </summary>
        private void ShowCurrentMetrics(IProfilingService profilingService, IConsoleContext context)
        {
            context.WriteLine("\n=== Current Performance Metrics ===");
            context.WriteLine($"FPS: {profilingService.CurrentFPS:F1}");
            context.WriteLine($"Memory: {(profilingService.CurrentMemoryUsage / (1024f * 1024f)):F1} MB");
            context.WriteLine($"Draw Calls: {profilingService.CurrentDrawCalls}");
            context.WriteLine($"Batches: {profilingService.CurrentBatches}");
            context.WriteLine($"Triangles: {FormatLargeNumber(profilingService.CurrentTriangles)}");
            context.WriteLine($"Vertices: {FormatLargeNumber(profilingService.CurrentVertices)}");
        }
        
        /// <summary>
        /// Starts a new profiling session
        /// </summary>
        private void HandleStartCommand(string[] args, IProfilingService profilingService, IConsoleContext context)
        {
            if (args.Length < 2)
            {
                context.WriteError("Start command requires frame count");
                context.WriteLine("Usage: profiling start <frame_count> [name]");
                return;
            }
            
            if (!int.TryParse(args[1], out int frameCount) || frameCount <= 0)
            {
                context.WriteError($"Invalid frame count: {args[1]}");
                return;
            }
            
            string sessionName = args.Length > 2 ? args[2] : null;
            
            profilingService.StartFrameSession(frameCount, sessionName);
            context.WriteLine($"Started frame-based profiling session for {frameCount} frames");
            
            // Hide console after starting session
            TryHideConsole();
        }
        
        /// <summary>
        /// Stops the current profiling session
        /// </summary>
        private void HandleStopCommand(IProfilingService profilingService, IConsoleContext context)
        {
            if (profilingService.IsSessionActive)
            {
                profilingService.StopSession();
                context.WriteLine("Profiling session stopped");
            }
            else
            {
                context.WriteWarning("No active profiling session to stop");
            }
        }
        
        /// <summary>
        /// Clears historical profiling data
        /// </summary>
        private void HandleClearCommand(IProfilingService profilingService, IConsoleContext context)
        {
            profilingService.ClearHistory();
            context.WriteLine("Historical profiling data cleared");
        }
        
        /// <summary>
        /// Configures profiling service settings
        /// </summary>
        private void HandleConfigCommand(string[] args, IProfilingService profilingService, IConsoleContext context)
        {
            if (args.Length < 3)
            {
                context.WriteError("Config command requires setting and value");
                context.WriteLine("Usage: profiling config update <interval_seconds>");
                return;
            }
            
            string setting = args[1].ToLower();
            
            if (!float.TryParse(args[2], out float interval) || interval <= 0f)
            {
                context.WriteError($"Invalid interval value: {args[2]}");
                return;
            }
            
            if (setting == "update")
            {
                profilingService.SetUpdateFrequency(interval);
                context.WriteLine($"Update frequency set to {interval:F1} seconds");
            }
            else
            {
                context.WriteError($"Invalid config setting: {setting}. Use 'update'");
            }
        }
        
        /// <summary>
        /// Shows the file path where profiling sessions are saved
        /// </summary>
        private void ShowFilePath(IConsoleContext context)
        {
            string path = Path.Combine(Application.persistentDataPath, "ProfilingSessions");
            context.WriteLine($"Profiling files saved to: {path}");
            
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.json");
                context.WriteLine($"Found {files.Length} session files");
                
                if (files.Length > 0)
                {
                    context.WriteLine("\nRecent files:");
                    System.Array.Sort(files, (x, y) => File.GetLastWriteTime(y).CompareTo(File.GetLastWriteTime(x)));
                    
                    for (int i = 0; i < Mathf.Min(5, files.Length); i++)
                    {
                        var fileInfo = new FileInfo(files[i]);
                        context.WriteLine($"  {Path.GetFileName(files[i])} ({fileInfo.LastWriteTime:yyyy-MM-dd HH:mm})");
                    }
                }
            }
            else
            {
                context.WriteLine("Directory does not exist yet (no sessions saved)");
            }
        }
        
        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Attempts to hide the console after starting a profiling session
        /// </summary>
        private void TryHideConsole()
        {
            try
            {
                var consoleService = GameManager.GetService<IConsoleService>();
                var eventSystem = GameManager.GetService<IEventSystem>();
                
                if (consoleService != null && eventSystem != null)
                {
                    eventSystem.Publish(new ConsoleToggleInputEvent(InputActionPhase.Performed));
                }
            }
            catch (System.Exception)
            {
                // Ignore console hiding failures - not critical
            }
        }
        
        /// <summary>
        /// Formats large numbers with appropriate suffixes (K, M)
        /// </summary>
        private string FormatLargeNumber(int number)
        {
            if (number >= 1000000)
                return (number / 1000000f).ToString("F1") + "M";
            else if (number >= 1000)
                return (number / 1000f).ToString("F1") + "K";
            else
                return number.ToString();
        }
        
        #endregion

        #region Command Information
        
        public override string GetUsage()
        {
            return "Usage: profiling <subcommand> [arguments]\n\n" +
                   "Subcommands:\n" +
                   "  status                    - Show profiling service status\n" +
                   "  current                   - Show current performance metrics\n" +
                   "  start <count> [name]      - Start frame-based session\n" +
                   "  stop                      - Stop current session\n" +
                   "  clear                     - Clear historical data\n" +
                   "  config update <interval>  - Set update frequency\n" +
                   "  path                      - Show file output location\n\n" +
                   "Examples:\n" +
                   "  profiling start 100           - Collect 100 frame samples\n" +
                   "  profiling start 500 \"test\"    - Profile 500 frames with name\n" +
                   "  profiling path                - Show where files are saved";
        }
        
        public override bool ValidateArgs(string[] args)
        {
            return true; // Allow all argument combinations - validation happens in Execute
        }
        
        #endregion
    }
}
