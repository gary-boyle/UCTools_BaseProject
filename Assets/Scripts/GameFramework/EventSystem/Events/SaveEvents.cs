using System;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events.Enums;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Save system events for game persistence
    /// Handles save requests, completion, and error states
    /// </summary>

    /// <summary>
    /// Unified event for all save operation requests
    /// Replaces RegularSaveRequestedEvent, AutoSaveRequestedEvent, and OverwriteSaveRequestedEvent
    /// </summary>
    public class SaveRequestedEvent
    {
        public SaveType SaveType { get; }
        public SaveFileInfo TargetSaveFile { get; }
        public DateTime RequestTime { get; }

        /// <summary>
        /// Constructor for Regular and Auto save requests
        /// </summary>
        /// <param name="saveType">Type of save operation</param>
        public SaveRequestedEvent(SaveType saveType)
        {
            if (saveType == SaveType.Overwrite)
                throw new ArgumentException("Overwrite save type requires target save file", nameof(saveType));

            SaveType = saveType;
            TargetSaveFile = null;
            RequestTime = DateTime.Now;
        }

        /// <summary>
        /// Constructor for Overwrite save requests
        /// </summary>
        /// <param name="targetSaveFile">Save file to overwrite</param>
        public SaveRequestedEvent(SaveFileInfo targetSaveFile)
        {
            SaveType = SaveType.Overwrite;
            TargetSaveFile = targetSaveFile ?? throw new ArgumentNullException(nameof(targetSaveFile));
            RequestTime = DateTime.Now;
        }

        /// <summary>
        /// Creates a regular save request event
        /// </summary>
        public static SaveRequestedEvent CreateRegularSave() => new SaveRequestedEvent(SaveType.Regular);

        /// <summary>
        /// Creates an auto-save request event
        /// </summary>
        public static SaveRequestedEvent CreateAutoSave() => new SaveRequestedEvent(SaveType.Auto);

        /// <summary>
        /// Creates an overwrite save request event
        /// </summary>
        public static SaveRequestedEvent CreateOverwriteSave(SaveFileInfo targetSaveFile) => new SaveRequestedEvent(targetSaveFile);
    }

    /// <summary>
    /// Event published when a save operation completes successfully
    /// </summary>
    public class SaveCompletedEvent
    {
        public string SaveFileName { get; }
        public SaveType SaveType { get; }
        public DateTime CompletionTime { get; }

        public SaveCompletedEvent(string saveFileName, SaveType saveType)
        {
            SaveFileName = saveFileName;
            SaveType = saveType;
            CompletionTime = DateTime.Now;
        }

        // Backwards compatibility properties
        public bool IsAutoSave => SaveType == SaveType.Auto;
        public bool IsOverwrite => SaveType == SaveType.Overwrite;
    }

    /// <summary>
    /// Event published when a save operation fails
    /// </summary>
    public class SaveFailedEvent
    {
        public string ErrorMessage { get; }
        public SaveType SaveType { get; }
        public Exception Exception { get; }
        public DateTime FailureTime { get; }

        public SaveFailedEvent(string errorMessage, SaveType saveType, Exception exception = null)
        {
            ErrorMessage = errorMessage;
            SaveType = saveType;
            Exception = exception;
            FailureTime = DateTime.Now;
        }

        // Backwards compatibility properties
        public bool IsAutoSave => SaveType == SaveType.Auto;
        public bool IsOverwrite => SaveType == SaveType.Overwrite;
    }
}
