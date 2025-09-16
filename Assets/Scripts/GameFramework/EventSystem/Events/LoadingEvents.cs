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
    /// Event triggered when the player requests to load a saved game
    /// </summary>
    public class LoadGameRequestedEvent
    {
        public string SaveFileName { get; set; }
        public SaveFileInfo SaveFileInfoOld { get; set; }
        
        public LoadGameRequestedEvent(string saveFileName, SaveFileInfo saveFileInfo)
        {
            SaveFileName = saveFileName;
            SaveFileInfoOld = saveFileInfo;
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
        public Exception Exception;
        
        public LoadingFailedEvent(Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }

    public class LoadingCompletedEvent
    {
    }
    
    /// <summary>
    /// Event published when loading process begins
    /// Allows UI and other services to prepare for loading
    /// </summary>
    public class LoadingStartedEvent
    {
        public SaveFileInfo SaveFileInfo { get; }

        public LoadingStartedEvent(SaveFileInfo saveFileInfo)
        {
            SaveFileInfo = saveFileInfo ?? throw new ArgumentNullException(nameof(saveFileInfo));
        }
    }
    
    /// <summary>
    /// Event published when loading should begin and transition to loading state
    /// </summary>
    public class BeginLoadGameEvent
    {
        public SaveFileInfo SaveFileInfo { get; }

        public BeginLoadGameEvent(SaveFileInfo saveFileInfo)
        {
            SaveFileInfo = saveFileInfo;
        }
    }

    /// <summary>
    /// Event published to update loading progress
    /// </summary>
    public class LoadingProgressEvent
    {
        public string Message { get; }
        public float Progress { get; }

        public LoadingProgressEvent(string message, float progress)
        {
            Message = message;
            Progress = Mathf.Clamp01(progress);
        }
    }

}
