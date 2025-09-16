using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Interfaces;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.DataStructures;
using GameFramework.FileSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Core;
using UnityEngine;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Console command for loading game state through the LoadService
    /// Integrates with EventSystem to request loads and track progress
    /// 
    /// Design:
    /// - Uses EventSystem to publish BeginLoadGameEvent
    /// - Subscribes to loading progress, completion, and failure events
    /// - Provides real-time loading feedback to user
    /// - Supports listing and selecting save files
    /// - Uses async FileService methods for better performance
    /// 
    /// Usage Examples:
    /// > load                    - List available save files
    /// > load -h                 - Show help information
    /// > load Save_001           - Load specific save file
    /// > load list               - List available save files (explicit)
    /// > load latest             - Load most recent save file
    /// > load auto               - Load most recent auto-save
    /// 
    /// Pros:
    /// - Fully integrated with existing load system architecture
    /// - Real-time progress feedback through event system
    /// - Convenient shortcuts (latest, auto) for common operations
    /// - Comprehensive error handling and validation
    /// - Event-driven design maintains system architecture
    /// - Async file operations for better performance
    /// 
    /// Cons:
    /// - Complex event subscription/unsubscription management
    /// - Async nature requires event-based feedback rather than direct returns
    /// - Requires multiple service dependencies
    /// - Loading progress events may be rapid for console display
    /// </summary>
    public class LoadCommand : ConsoleCommandBase
    {
        #region Command Properties
        
        public override string CommandName => "load";
        public override string Description => "Load game state from save files (use -h for detailed help)";
        public override CategoryEnum Category => CategoryEnum.System;
        public override int Tag => 100;
        
        #endregion

        #region Services and State
        
        private IEventSystem _eventSystem;
        private IFileService _fileService;
        private IConsoleContext _currentContext;
        private bool _isWaitingForLoadResult = false;
        private string _currentLoadingFile = "";
        
        /// <summary>
        /// Lazy initialization of services to avoid circular dependencies
        /// </summary>
        private void EnsureServicesInitialized()
        {
            if (_eventSystem == null)
            {
                _eventSystem = GameManager.GetService<IEventSystem>();
            }
            
            if (_fileService == null)
            {
                _fileService = GameManager.GetService<IFileService>();
            }
        }
        
        #endregion

        #region Command Execution
        
        public override void Execute(string[] args, IConsoleContext context)
        {
            // Check for help flag first
            if (ContainsHelpFlag(args))
            {
                DisplayDetailedHelp(context);
                return;
            }
            
            // Ensure services are available
            EnsureServicesInitialized();
            
            if (_eventSystem == null)
            {
                context.WriteError("EventSystem is not available. Ensure the service is properly initialized.");
                return;
            }
            
            if (_fileService == null)
            {
                context.WriteError("FileService is not available. Ensure the service is properly initialized.");
                return;
            }
            
            // Store context for event callbacks
            _currentContext = context;
            
            // Handle different argument patterns
            switch (args.Length)
            {
                case 0:
                    // No arguments - list available save files
                    _ = ListSaveFilesAsync(context);
                    break;
                    
                case 1:
                    // One argument - filename or special command
                    HandleSingleArgument(args[0], context);
                    break;
                    
                default:
                    context.WriteError("Too many arguments provided.");
                    context.WriteLine("Use 'load -h' for detailed usage information.");
                    break;
            }
        }
        
        #endregion

        #region Help System
        
        private bool ContainsHelpFlag(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        
        private void DisplayDetailedHelp(IConsoleContext context)
        {
            context.WriteLine("=== LOAD COMMAND HELP ===");
            context.WriteLine("");
            context.WriteLine("DESCRIPTION:");
            context.WriteLine("  Load saved game state using the integrated LoadService.");
            context.WriteLine("  Provides real-time loading progress and comprehensive error handling.");
            context.WriteLine("");
            
            context.WriteLine("BASIC USAGE:");
            context.WriteLine("  load                     - List all available save files");
            context.WriteLine("  load -h                  - Show this help information");
            context.WriteLine("  load <filename>          - Load specific save file");
            context.WriteLine("");
            
            context.WriteLine("LOAD OPTIONS:");
            context.WriteLine("  load list                - List all available save files (explicit)");
            context.WriteLine("  load latest              - Load most recent save file");
            context.WriteLine("  load auto                - Load most recent auto-save file");
            context.WriteLine("  load Save_001            - Load specific save file by name");
            context.WriteLine("");
            
            context.WriteLine("EXAMPLES:");
            context.WriteLine("  load                     - See all available saves");
            context.WriteLine("  load latest              - Quick load most recent save");
            context.WriteLine("  load auto                - Load latest auto-save");
            context.WriteLine("  load Save_2024-01-15     - Load specific save file");
            context.WriteLine("");
            
            context.WriteLine("FILE NAMING:");
            context.WriteLine("  • Specify filename without .json extension");
            context.WriteLine("  • Case-insensitive matching");
            context.WriteLine("  • Partial names may work if unique");
            context.WriteLine("");
            
            context.WriteLine("LOADING PROCESS:");
            context.WriteLine("  1. File validation and reading");
            context.WriteLine("  2. Save data validation");
            context.WriteLine("  3. Data conversion to game objects");
            context.WriteLine("  4. Application to current game state");
            context.WriteLine("  5. Progress updates shown in real-time");
            context.WriteLine("");
            
            context.WriteLine("NOTES:");
            context.WriteLine("  • Loading will overwrite current game state");
            context.WriteLine("  • Progress updates shown during load process");
            context.WriteLine("  • Loading is asynchronous with event-based feedback");
            context.WriteLine("  • Failed loads preserve current game state");
            context.WriteLine("  • Save files located in: " + Application.persistentDataPath + "/Saves/");
            context.WriteLine("");
            
            context.WriteLine("=== END HELP ===");
        }
        
        #endregion

        #region Argument Handlers
        
        private void HandleSingleArgument(string arg, IConsoleContext context)
        {
            switch (arg.ToLower())
            {
                case "list":
                    _ = ListSaveFilesAsync(context);
                    break;
                    
                case "latest":
                    _ = LoadLatestSaveAsync(context);
                    break;
                    
                case "auto":
                    _ = LoadLatestAutoSaveAsync(context);
                    break;
                    
                default:
                    // Treat as filename
                    _ = LoadSpecificFileAsync(arg, context);
                    break;
            }
        }
        
        #endregion

        #region Load Operations
        
        /// <summary>
        /// Asynchronously list save files using FileService.GetSaveFilesAsync()
        /// </summary>
        private async Task ListSaveFilesAsync(IConsoleContext context)
        {
            try
            {
                context.WriteLine("Loading save files...");
                var saveFiles = await _fileService.GetSaveFilesAsync();
                
                if (saveFiles == null || saveFiles.Length == 0)
                {
                    context.WriteLine("No save files found.");
                    context.WriteLine("Use 'save' command to create save files.");
                    return;
                }
                
                context.WriteLine($"Found {saveFiles.Length} save file(s):");
                context.WriteLine("");
                
                // Sort by LastSaveTime (ticks) - newest first
                var sortedSaves = new List<SaveFileInfo>(saveFiles);
                sortedSaves.Sort((a, b) => b.LastSaveTime.CompareTo(a.LastSaveTime));
                
                var latestSave = sortedSaves.First();
                var latestAuto = sortedSaves.FirstOrDefault(s => s.WasAutoSaved);
                
                foreach (var saveFile in sortedSaves)
                {
                    string fileType = saveFile.WasAutoSaved ? "Auto" : "Manual";
                    string fileName = saveFile.FileName.Replace(".json", "");
                    string indicator = "";
                    
                    if (saveFile == latestSave && saveFile == latestAuto)
                        indicator = " (Latest & Latest Auto)";
                    else if (saveFile == latestSave)
                        indicator = " (Latest)";
                    else if (saveFile == latestAuto)
                        indicator = " (Latest Auto)";
                    
                    // Convert ticks to DateTime for display
                    context.WriteLine($"  {fileName} ({fileType}) - {saveFile.LastSaveTime:yyyy-MM-dd HH:mm:ss}{indicator}");
                }
                
                context.WriteLine("");
                context.WriteLine("Use 'load <filename>' to load a specific save.");
                context.WriteLine("Use 'load latest' or 'load auto' for quick loading.");
            }
            catch (Exception ex)
            {
                context.WriteError($"Failed to list save files: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Asynchronously load the most recent save file
        /// </summary>
        private async Task LoadLatestSaveAsync(IConsoleContext context)
        {
            try
            {
                context.WriteLine("Finding latest save file...");
                var saveFiles = await _fileService.GetSaveFilesAsync();
                if (saveFiles == null || saveFiles.Length == 0)
                {
                    context.WriteError("No save files available to load.");
                    return;
                }
                
                // Find latest save by LastSaveTime (ticks)
                var latestSave = saveFiles.OrderByDescending(s => s.LastSaveTime).First();
                LoadSaveFile(latestSave, context);
            }
            catch (Exception ex)
            {
                context.WriteError($"Failed to load latest save: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Asynchronously load the most recent auto-save file
        /// </summary>
        private async Task LoadLatestAutoSaveAsync(IConsoleContext context)
        {
            try
            {
                context.WriteLine("Finding latest auto-save file...");
                var saveFiles = await _fileService.GetSaveFilesAsync();
                if (saveFiles == null || saveFiles.Length == 0)
                {
                    context.WriteError("No save files available to load.");
                    return;
                }
                
                // Find latest auto-save
                var autoSaves = saveFiles.Where(s => s.WasAutoSaved).ToList();
                if (autoSaves.Count == 0)
                {
                    context.WriteError("No auto-save files found.");
                    context.WriteLine("Use 'load latest' to load the most recent manual save.");
                    return;
                }
                
                var latestAutoSave = autoSaves.OrderByDescending(s => s.LastSaveTime).First();
                LoadSaveFile(latestAutoSave, context);
            }
            catch (Exception ex)
            {
                context.WriteError($"Failed to load latest auto-save: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Asynchronously load a specific save file by name
        /// </summary>
        private async Task LoadSpecificFileAsync(string fileName, IConsoleContext context)
        {
            try
            {
                context.WriteLine("Searching for save file...");
                var saveFiles = await _fileService.GetSaveFilesAsync();
                if (saveFiles == null || saveFiles.Length == 0)
                {
                    context.WriteError("No save files available to load.");
                    return;
                }
                
                // Add .json extension if not present
                string searchName = fileName;
                if (!searchName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    searchName += ".json";
                }
                
                // Find matching save file (case-insensitive)
                var matchingSave = saveFiles.FirstOrDefault(s => 
                    s.FileName.Equals(searchName, StringComparison.OrdinalIgnoreCase));
                
                if (matchingSave == null)
                {
                    // Try partial matching
                    var partialMatches = saveFiles.Where(s => 
                        s.FileName.ToLower().Contains(fileName.ToLower())).ToList();
                    
                    if (partialMatches.Count == 0)
                    {
                        context.WriteError($"Save file not found: {fileName}");
                        context.WriteLine("Use 'load list' to see available save files.");
                        return;
                    }
                    
                    if (partialMatches.Count > 1)
                    {
                        context.WriteError($"Multiple files match '{fileName}':");
                        foreach (var match in partialMatches)
                        {
                            context.WriteLine($"  {match.FileName.Replace(".json", "")}");
                        }
                        context.WriteLine("Please be more specific.");
                        return;
                    }
                    
                    matchingSave = partialMatches.First();
                }
                
                LoadSaveFile(matchingSave, context);
            }
            catch (Exception ex)
            {
                context.WriteError($"Failed to load save file '{fileName}': {ex.Message}");
            }
        }
        
        private void LoadSaveFile(SaveFileInfo saveFileInfo, IConsoleContext context)
        {
            try
            {
                // Subscribe to loading events for progress feedback
                SubscribeToLoadEvents();
                
                _isWaitingForLoadResult = true;
                _currentLoadingFile = saveFileInfo.FileName;
                
                context.WriteLine($"Starting load: {saveFileInfo.FileName.Replace(".json", "")}");
                
                // Convert ticks to DateTime for display
                context.WriteLine($"Save date: {saveFileInfo.LastSaveTime:yyyy-MM-dd HH:mm:ss} ({(saveFileInfo.WasAutoSaved ? "Auto" : "Manual")})");
                
                // Publish load event
                var loadEvent = new BeginLoadGameEvent(saveFileInfo);
                _eventSystem.Publish(loadEvent);
            }
            catch (Exception ex)
            {
                context.WriteError($"Failed to start load operation: {ex.Message}");
                _isWaitingForLoadResult = false;
                UnsubscribeFromLoadEvents();
            }
        }
        
        #endregion

        #region Event Handling
        
        private void SubscribeToLoadEvents()
        {
            _eventSystem?.Subscribe<LoadingProgressEvent>(OnLoadingProgress);
            _eventSystem?.Subscribe<LoadingCompletedEvent>(OnLoadingCompleted);
            _eventSystem?.Subscribe<LoadingFailedEvent>(OnLoadingFailed);
        }
        
        private void UnsubscribeFromLoadEvents()
        {
            _eventSystem?.Unsubscribe<LoadingProgressEvent>(OnLoadingProgress);
            _eventSystem?.Unsubscribe<LoadingCompletedEvent>(OnLoadingCompleted);
            _eventSystem?.Unsubscribe<LoadingFailedEvent>(OnLoadingFailed);
        }
        
        private void OnLoadingProgress(LoadingProgressEvent evt)
        {
            if (!_isWaitingForLoadResult || _currentContext == null) return;
            
            // Show progress with percentage
            int percentage = Mathf.RoundToInt(evt.Progress * 100);
            _currentContext.WriteLine($"  [{percentage:D3}%] {evt.Message}");
        }
        
        private void OnLoadingCompleted(LoadingCompletedEvent evt)
        {
            if (!_isWaitingForLoadResult || _currentContext == null) return;
            
            _currentContext.WriteLine($"✓ Load completed successfully: {_currentLoadingFile.Replace(".json", "")}");
            _currentContext.WriteLine("  Game state has been restored from save file.");
            
            // Clean up
            _isWaitingForLoadResult = false;
            _currentContext = null;
            _currentLoadingFile = "";
            UnsubscribeFromLoadEvents();
        }
        
        private void OnLoadingFailed(LoadingFailedEvent evt)
        {
            if (!_isWaitingForLoadResult || _currentContext == null) return;
            
            _currentContext.WriteError($"✗ Load failed: {evt.Exception?.Message ?? "Unknown error"}");
            _currentContext.WriteLine("  Current game state has been preserved.");
            
            if (evt.Exception != null)
            {
                _currentContext.WriteError($"  Exception: {evt.Exception.GetType().Name}");
            }
            
            // Clean up
            _isWaitingForLoadResult = false;
            _currentContext = null;
            _currentLoadingFile = "";
            UnsubscribeFromLoadEvents();
        }
        
        #endregion

        #region Documentation and Validation
        
        public override string GetUsage()
        {
            return "Usage: load [filename|option] | load -h\n" +
                   "\n" +
                   "Quick examples:\n" +
                   "  load                     - List available save files\n" +
                   "  load -h                  - Show detailed help\n" +
                   "  load latest              - Load most recent save\n" +
                   "  load auto                - Load latest auto-save\n" +
                   "  load Save_001            - Load specific save file\n" +
                   "\n" +
                   "Use 'load -h' for comprehensive usage information.";
        }
        
        public override bool ValidateArgs(string[] args)
        {
            // Always return true since -h flag takes precedence
            return true;
        }
        
        #endregion
    }
}
