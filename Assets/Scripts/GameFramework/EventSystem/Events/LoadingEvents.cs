using System;
using System.Collections.Generic;
using GameFramework.DataStructures;
using GameFramework.StateMachine.Enum;
using UnityEngine;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Loading system events for game data loading and progress tracking
    /// Handles load requests, progress updates, and completion states
    /// </summary>
    
    /// <summary>
    /// Event for loading progress updates with message and completion percentage
    /// Consolidates progress tracking for UI and system notifications
    /// </summary>
    public class LoadingProgressEvent
    {
        public string Message { get; }
        public float Progress { get; }
    
        public LoadingProgressEvent(string message, float progress)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Progress = Mathf.Clamp01(progress); // Ensure progress is between 0 and 1
        }
    }
    
    public class GameSystemsInitializedEvent
    {
        public LoadingType LoadingType { get; set; }
        public Dictionary<string, object> GameData { get; set; }
    }
    
    /// <summary>
    /// Event triggered when the player requests to load a saved game
    /// </summary>
    public class LoadGameRequestedEvent
    {
        public string SaveFileName { get; set; }
        public SaveFileInfo SaveFileInfo { get; set; }
        
        public LoadGameRequestedEvent(string saveFileName, SaveFileInfo saveFileInfo)
        {
            SaveFileName = saveFileName;
            SaveFileInfo = saveFileInfo;
        }
    }
    
    public class LoadSaveFileEvent
    {
        public SaveFileInfo SaveFileInfo { get; }

        public LoadSaveFileEvent(SaveFileInfo saveFileInfo)
        {
            SaveFileInfo = saveFileInfo ?? throw new ArgumentNullException(nameof(saveFileInfo));
        }
    }

    public class LoadingMessageChangedEvent
    {
        public string Message { get; }
    
        public LoadingMessageChangedEvent(string message)
        {
            Message = message;
        }
    }

    public class LoadingFailedEvent
    {
        public Exception Exception { get; }
        public string ErrorMessage => Exception?.Message ?? "Unknown loading error";
    
        public LoadingFailedEvent(Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }

    public class LoadingCompletedEvent
    {
        public GameSession Session { get; }
    
        public LoadingCompletedEvent(GameSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }
    }
    
    /// <summary>
    /// Event published when loading process begins
    /// Allows UI and other services to prepare for loading
    /// </summary>
    public class LoadingStartedEvent
    {
        public SaveFileInfo SaveFileInfo { get; }
        public DateTime StartTime { get; }

        public LoadingStartedEvent(SaveFileInfo saveFileInfo)
        {
            SaveFileInfo = saveFileInfo ?? throw new ArgumentNullException(nameof(saveFileInfo));
            StartTime = DateTime.Now;
        }
    }
}
