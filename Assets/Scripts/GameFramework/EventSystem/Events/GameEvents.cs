using System;
using System.Collections.Generic;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.StateMachine.Enum;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework.EventSystem.Events
{
    // Game Events
    public class GameStateChangeEvent
    {
        public GameStateType PreviousState { get; set; }
        public GameStateType NewState { get; set; }
        public GameContext Context { get; set; }
    }

    public class GamePausedEvent { }

    public class GameResumedEvent { }
    public class OptionsChangedEvent { }
    public class SaveGameEvent { }
    public class LoadGameEvent { }

    public class NewGameRequestedEvent
    {
        public string PlayerName { get; set; }
        public string Difficulty { get; set; } = "Normal";
        public string StartingScene { get; set; } = "GameLevel1";
        public Dictionary<string, object> CustomData { get; set; } = new();
    }

    public class SceneLoadedEvent
    {
        public string SceneName { get; set; }
    }
    public class LoadWindowRequestedEvent { }
    public class OptionsRequestedEvent { }
    public class CreditsRequestedEvent { }
    public class QuitRequestedEvent { }
    public class PauseRequestedEvent { }
    public class ResumeRequestedEvent { }
    public class MainMenuRequestedEvent { }
    public class GameOverEvent { }
    public class VictoryEvent { }
    public class UICancelInputEvent { }

    #region Player Input Events
    
    public class PlayerMoveInputEvent
    {
        public Vector2 MovementVector { get; }
        public InputActionPhase Phase { get; }
        
        public PlayerMoveInputEvent(Vector2 movementVector, InputActionPhase phase)
        {
            MovementVector = movementVector;
            Phase = phase;
        }
    }
    
    public class PlayerLookInputEvent
    {
        public Vector2 LookDelta { get; }
        public InputActionPhase Phase { get; }
        
        public PlayerLookInputEvent(Vector2 lookDelta, InputActionPhase phase)
        {
            LookDelta = lookDelta;
            Phase = phase;
        }
    }
    
    public class PlayerAttackInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerAttackInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class PlayerInteractInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerInteractInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class PlayerCrouchInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerCrouchInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class PlayerJumpInputEvent
    {
        // Jump is typically just performed, no need for phase
        public PlayerJumpInputEvent() { }
    }

    public class PlayerPreviousInputEvent { }


    public class PlayerNextInputEvent { }
    
    public class PlayerSprintInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerSprintInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class PlayerPauseInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public PlayerPauseInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }
    
    #endregion
    
    #region UI Input Events
    
    public class UINavigateInputEvent
    {
        public Vector2 NavigationVector { get; }
        
        public UINavigateInputEvent(Vector2 navigationVector)
        {
            NavigationVector = navigationVector;
        }
    }
    
    public class UISubmitInputEvent { }

    public class UIPointInputEvent
    {
        public Vector2 Position { get; }
        
        public UIPointInputEvent(Vector2 position)
        {
            Position = position;
        }
    }
    
    public class UIClickInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public UIClickInputEvent(InputActionPhase phase)
        {
            Phase = phase;
        }
    }
    
    public class UIRightClickInputEvent { }
    
    public class UIMiddleClickInputEvent { }
    
    public class UIScrollWheelInputEvent
    {
        public Vector2 ScrollDelta { get; }
        
        public UIScrollWheelInputEvent(Vector2 scrollDelta)
        {
            ScrollDelta = scrollDelta;
        }
    }

    #endregion
    
    #region Console Events
    public class ConsoleToggleInputEvent
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleToggleInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }

    public class ConsoleSubmitInputEvent 
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleSubmitInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }

    public class ConsoleTabCompleteInputEvent 
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleTabCompleteInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }

    public class ConsoleHistoryUpInputEvent 
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleHistoryUpInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }

    public class ConsoleHistoryDownInputEvent 
    {
        public InputActionPhase Phase { get; }
        
        public ConsoleHistoryDownInputEvent(InputActionPhase phase = InputActionPhase.Performed)
        {
            Phase = phase;
        }
    }
    #endregion
    
    #region Save Events

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

    #endregion

    #region Loading Events
    public class LoadingProgressEvent
    {
        public float Progress { get; set; }
        public string Message { get; set; }
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
        public string SaveFileName => SaveFileInfo.fileName;

        public LoadSaveFileEvent(SaveFileInfo saveFileInfo)
        {
            SaveFileInfo = saveFileInfo ?? throw new ArgumentNullException(nameof(saveFileInfo));
        }
    }
    public class LoadingProgressChangedEvent
    {
        public string Message { get; }
        public float Progress { get; }
    
        public LoadingProgressChangedEvent(string message, float progress)
        {
            Message = message;
            Progress = progress;
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
        public string SaveFileName => SaveFileInfo.fileName;
        public DateTime StartTime { get; }

        public LoadingStartedEvent(SaveFileInfo saveFileInfo)
        {
            SaveFileInfo = saveFileInfo ?? throw new ArgumentNullException(nameof(saveFileInfo));
            StartTime = DateTime.Now;
        }
    }
    #endregion
    
    #region Session Management Events

    public class SessionCreatedEvent
    {
        public GameSession Session { get; }
    
        public SessionCreatedEvent(GameSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }
    }

    public class SessionLoadedEvent
    {
        public GameSession Session { get; }
    
        public SessionLoadedEvent(GameSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }
    }

    public class SessionClearedEvent
    {
        public string PlayerName { get; }
    
        public SessionClearedEvent(string playerName = null)
        {
            PlayerName = playerName;
        }
    }

    #endregion
}