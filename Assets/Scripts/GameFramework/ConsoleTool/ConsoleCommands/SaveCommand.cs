using GameFramework.ConsoleTool.Enums;
using GameFramework.ConsoleTool.Interfaces;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Events.Enums;
using GameFramework.DataStructures;
using GameFramework.FileSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Core;
using UnityEngine;

namespace GameFramework.ConsoleTool.Commands
{
    /// <summary>
    /// Console command for saving game state through the SaveService
    /// Integrates with EventSystem to request saves and receive feedback
    /// 
    /// Design:
    /// - Uses EventSystem to publish SaveRequestedEvent for different save types
    /// - Subscribes to save completion/failure events for user feedback
    /// - Supports regular, auto, and overwrite save operations
    /// - Provides comprehensive help via -h flag
    /// - Uses async FileService methods for save file operations
    /// 
    /// Usage Examples:
    /// > save                    - Perform regular save
    /// > save -h                 - Show help information
    /// > save auto               - Perform auto-save
    /// > save regular            - Perform regular save (explicit)
    /// > save overwrite Save_001 - Overwrite specific save file
    /// > save list               - List available save files
    /// 
    /// Pros:
    /// - Fully integrated with existing save system architecture
    /// - Event-driven design maintains loose coupling
    /// - Comprehensive error handling and user feedback
    /// - Supports all save types from SaveService
    /// - Real-time feedback through event subscription
    /// - Async file operations for better performance
    /// 
    /// Cons:
    /// - Requires EventSystem and FileService dependencies
    /// - Async nature means feedback comes through events rather than return values
    /// - More complex than direct service calls due to event-driven architecture
    /// </summary>
    public class SaveCommand : ConsoleCommandBase
    {
        #region Command Properties
        
        public override string CommandName => "save";
        public override string Description => "Save game state (use -h for detailed help)";
        public override CategoryEnum Category => CategoryEnum.System;
        public override int Tag => 100;
        
        #endregion

        #region Services and State
        
        private IEventSystem _eventSystem;
        private IFileService _fileService;
        private IConsoleContext _currentContext;
        private bool _isWaitingForSaveResult = false;
        
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
            
            // Store context for event callbacks
            _currentContext = context;
            
            // Subscribe to save events for feedback
            SubscribeToSaveEvents();
            
            // Handle different argument patterns
            switch (args.Length)
            {
                case 0:
                    // No arguments - perform regular save
                    PerformSave(SaveType.Regular, context);
                    break;
                    
                case 1:
                    // One argument - save type or special command
                    HandleSingleArgument(args[0], context);
                    break;
                    
                case 2:
                    // Two arguments - operation with parameter
                    HandleTwoArguments(args[0], args[1], context);
                    break;
                    
                default:
                    context.WriteError("Too many arguments provided.");
                    context.WriteLine("Use 'save -h' for detailed usage information.");
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
            context.WriteLine("=== SAVE COMMAND HELP ===");
            context.WriteLine("");
            context.WriteLine("DESCRIPTION:");
            context.WriteLine("  Save current game state using the integrated SaveService.");
            context.WriteLine("  All operations are event-driven and provide real-time feedback.");
            context.WriteLine("");
            
            context.WriteLine("BASIC USAGE:");
            context.WriteLine("  save                     - Perform regular save (creates new save file)");
            context.WriteLine("  save -h                  - Show this help information");
            context.WriteLine("  save list                - List all available save files");
            context.WriteLine("");
            
            context.WriteLine("SAVE TYPES:");
            context.WriteLine("  save regular             - Create new regular save file");
            context.WriteLine("  save auto                - Perform auto-save (overwrites existing auto-save)");
            context.WriteLine("  save overwrite <file>    - Overwrite specific save file");
            context.WriteLine("");
            
            context.WriteLine("EXAMPLES:");
            context.WriteLine("  save                     - Quick regular save");
            context.WriteLine("  save auto                - Auto-save current progress");
            context.WriteLine("  save overwrite Save_001  - Overwrite 'Save_001.json'");
            context.WriteLine("  save list                - See all available save files");
            context.WriteLine("");
            
            context.WriteLine("SAVE FILE NAMING:");
            context.WriteLine("  Regular saves: Save_YYYY-MM-DD_HH-mm-ss.json");
            context.WriteLine("  Auto saves: AutoSave_<PlayerID>.json or AutoSave_YYYY-MM-DD_HH-mm-ss.json");
            context.WriteLine("  Overwrite: Uses existing filename");
            context.WriteLine("");
            
            context.WriteLine("NOTES:");
            context.WriteLine("  • Save operations are asynchronous - feedback provided via events");
            context.WriteLine("  • Auto-saves use player unique ID when available");
            context.WriteLine("  • Overwrite operations require exact filename (without .json extension)");
            context.WriteLine("  • All saves include game session data and player data");
            context.WriteLine("  • Save files are stored in: " + Application.persistentDataPath + "/Saves/");
            context.WriteLine("");
            
            context.WriteLine("=== END HELP ===");
        }
        
        #endregion

        #region Argument Handlers
        
        private void HandleSingleArgument(string arg, IConsoleContext context)
        {
            switch (arg.ToLower())
            {
                case "regular":
                    PerformSave(SaveType.Regular, context);
                    break;
                    
                case "auto":
                    PerformSave(SaveType.Auto, context);
                    break;
                    
                case "list":
                    // Use async method for listing save files
                    _ = ListSaveFilesAsync(context);
                    break;
                    
                default:
                    context.WriteError($"Unknown save command: '{arg}'");
                    context.WriteLine("Valid options: regular, auto, list");
                    context.WriteLine("Use 'save -h' for detailed help.");
                    break;
            }
        }
        
        private void HandleTwoArguments(string operation, string parameter, IConsoleContext context)
        {
            switch (operation.ToLower())
            {
                case "overwrite":
                    PerformOverwriteSave(parameter, context);
                    break;
                    
                default:
                    context.WriteError($"Unknown save operation: '{operation}'");
                    context.WriteLine("Valid operations: overwrite <filename>");
                    context.WriteLine("Use 'save -h' for detailed help.");
                    break;
            }
        }
        
        #endregion

        #region Save Operations
        
        private void PerformSave(SaveType saveType, IConsoleContext context)
        {
            try
            {
                _isWaitingForSaveResult = true;
                SaveRequestedEvent saveEvent;
                
                switch (saveType)
                {
                    case SaveType.Regular:
                        saveEvent = SaveRequestedEvent.CreateRegularSave();
                        context.WriteLine("Requesting regular save...");
                        break;
                        
                    case SaveType.Auto:
                        saveEvent = SaveRequestedEvent.CreateAutoSave();
                        context.WriteLine("Requesting auto-save...");
                        break;
                        
                    default:
                        context.WriteError($"Unsupported save type: {saveType}");
                        return;
                }
                
                _eventSystem.Publish(saveEvent);
            }
            catch (Exception ex)
            {
                context.WriteError($"Failed to request save: {ex.Message}");
                _isWaitingForSaveResult = false;
            }
        }
        
        private void PerformOverwriteSave(String fileName, IConsoleContext context)
        {
            try
            {
                // Validate filename and create SaveFileInfo
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    context.WriteError("Filename cannot be empty for overwrite operation.");
                    return;
                }
                
                // Add .json extension if not present
                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".json";
                }
                
                // Create SaveFileInfo for the target file
                var saveFileInfo = new SaveFileInfo
                {
                    FileName = fileName,
                    // Note: Other properties like LastSaveTime, etc. will be updated during save
                };
                
                _isWaitingForSaveResult = true;
                var saveEvent = SaveRequestedEvent.CreateOverwriteSave(saveFileInfo);
                
                context.WriteLine($"Requesting overwrite save for: {fileName}");
                _eventSystem.Publish(saveEvent);
            }
            catch (Exception ex)
            {
                context.WriteError($"Failed to request overwrite save: {ex.Message}");
                _isWaitingForSaveResult = false;
            }
        }
        
        /// <summary>
        /// Asynchronously list save files using FileService.GetSaveFilesAsync()
        /// </summary>
        private async Task ListSaveFilesAsync(IConsoleContext context)
        {
            if (_fileService == null)
            {
                context.WriteError("FileService is not available.");
                return;
            }
            
            try
            {
                context.WriteLine("Loading save files...");
                var saveFiles = await _fileService.GetSaveFilesAsync();
                
                if (saveFiles == null || saveFiles.Length == 0)
                {
                    context.WriteLine("No save files found.");
                    return;
                }
                
                context.WriteLine($"Found {saveFiles.Length} save file(s):");
                context.WriteLine("");
                
                // Sort by LastSaveTime (ticks) - newest first
                var sortedSaves = new List<SaveFileInfo>(saveFiles);
                sortedSaves.Sort((a, b) => b.LastSaveTime.CompareTo(a.LastSaveTime));
                
                foreach (var saveFile in sortedSaves)
                {
                    string fileType = saveFile.WasAutoSaved ? "Auto" : "Manual";
                    // Convert ticks to DateTime for display
                    context.WriteLine($"  {saveFile.FileName} ({fileType}) - {saveFile.LastSaveTime:yyyy-MM-dd HH:mm:ss}");
                }
                
                context.WriteLine("");
                context.WriteLine("Use 'save overwrite <filename>' to overwrite a specific save file.");
            }
            catch (Exception ex)
            {
                context.WriteError($"Failed to list save files: {ex.Message}");
            }
        }
        
        #endregion

        #region Event Handling
        
        private void SubscribeToSaveEvents()
        {
            _eventSystem?.Subscribe<SaveCompletedEvent>(OnSaveCompleted);
            _eventSystem?.Subscribe<SaveFailedEvent>(OnSaveFailed);
        }
        
        private void UnsubscribeFromSaveEvents()
        {
            _eventSystem?.Unsubscribe<SaveCompletedEvent>(OnSaveCompleted);
            _eventSystem?.Unsubscribe<SaveFailedEvent>(OnSaveFailed);
        }
        
        private void OnSaveCompleted(SaveCompletedEvent evt)
        {
            if (!_isWaitingForSaveResult || _currentContext == null) return;
            
            string saveTypeText = evt.SaveType.ToString().ToLower();
            _currentContext.WriteLine($"✓ {char.ToUpper(saveTypeText[0])}{saveTypeText.Substring(1)} save completed successfully: {evt.SaveFileName}");
            _currentContext.WriteLine($"  Saved at: {evt.CompletionTime:yyyy-MM-dd HH:mm:ss}");
            
            // Clean up
            _isWaitingForSaveResult = false;
            _currentContext = null;
            UnsubscribeFromSaveEvents();
        }
        
        private void OnSaveFailed(SaveFailedEvent evt)
        {
            if (!_isWaitingForSaveResult || _currentContext == null) return;
            
            string saveTypeText = evt.SaveType.ToString().ToLower();
            _currentContext.WriteError($"✗ {char.ToUpper(saveTypeText[0])}{saveTypeText.Substring(1)} save failed: {evt.ErrorMessage}");
            
            if (evt.Exception != null)
            {
                _currentContext.WriteError($"  Exception: {evt.Exception.GetType().Name}");
            }
            
            // Clean up
            _isWaitingForSaveResult = false;
            _currentContext = null;
            UnsubscribeFromSaveEvents();
        }
        
        #endregion

        #region Documentation and Validation
        
        public override string GetUsage()
        {
            return "Usage: save [type|operation] [parameter] | save -h\n" +
                   "\n" +
                   "Quick examples:\n" +
                   "  save                     - Perform regular save\n" +
                   "  save -h                  - Show detailed help\n" +
                   "  save auto                - Perform auto-save\n" +
                   "  save overwrite Save_001  - Overwrite specific save\n" +
                   "  save list                - List available saves\n" +
                   "\n" +
                   "Use 'save -h' for comprehensive usage information.";
        }
        
        public override bool ValidateArgs(string[] args)
        {
            // Always return true since -h flag takes precedence
            return true;
        }
        
        #endregion
    }
}
