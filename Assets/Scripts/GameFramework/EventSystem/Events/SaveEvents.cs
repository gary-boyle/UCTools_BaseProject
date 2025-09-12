using System;
using GameFramework.DataStructures;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Save system events for game persistence
    /// Handles save requests, completion, and error states
    /// </summary>

    /// <summary>
    /// Event triggered when the player requests a regular save operation
    /// </summary>
    public class RegularSaveRequestedEvent
    {
        public DateTime RequestTime { get; }
        
        public RegularSaveRequestedEvent()
        {
            RequestTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event triggered when the player requests an auto-save operation
    /// </summary>
    public class AutoSaveRequestedEvent
    {
        public DateTime RequestTime { get; }
        
        public AutoSaveRequestedEvent()
        {
            RequestTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event triggered when the player requests to overwrite an existing save file
    /// </summary>
    public class OverwriteSaveRequestedEvent
    {
        public SaveFileInfo TargetSaveFile { get; }
        public DateTime RequestTime { get; }

        public OverwriteSaveRequestedEvent(SaveFileInfo targetSaveFile)
        {
            TargetSaveFile = targetSaveFile ?? throw new ArgumentNullException(nameof(targetSaveFile));
            RequestTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when a save operation completes successfully
    /// </summary>
    public class SaveCompletedEvent
    {
        public string SaveFileName { get; }
        public bool IsAutoSave { get; }
        public bool IsOverwrite { get; }
        public DateTime CompletionTime { get; }

        public SaveCompletedEvent(string saveFileName, bool isAutoSave, bool isOverwrite)
        {
            SaveFileName = saveFileName;
            IsAutoSave = isAutoSave;
            IsOverwrite = isOverwrite;
            CompletionTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when a save operation fails
    /// </summary>
    public class SaveFailedEvent
    {
        public string ErrorMessage { get; }
        public bool IsAutoSave { get; }
        public bool IsOverwrite { get; }
        public Exception Exception { get; }
        public DateTime FailureTime { get; }

        public SaveFailedEvent(string errorMessage, bool isAutoSave, bool isOverwrite, Exception exception = null)
        {
            ErrorMessage = errorMessage;
            IsAutoSave = isAutoSave;
            IsOverwrite = isOverwrite;
            Exception = exception;
            FailureTime = DateTime.Now;
        }
    }
}
