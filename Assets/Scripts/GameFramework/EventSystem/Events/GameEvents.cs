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
    public class GameStartedEvent { }
    public class GameEndedEvent { }
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
    
    public class PlayerPreviousInputEvent
    {
        // Previous is typically just performed
        public PlayerPreviousInputEvent() { }
    }
    
    public class PlayerNextInputEvent
    {
        // Next is typically just performed
        public PlayerNextInputEvent() { }
    }
    
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
    
    public class UISubmitInputEvent
    {
        // Submit is typically just performed
        public UISubmitInputEvent() { }
    }

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
    
    public class UIRightClickInputEvent
    {
        // Right click is typically just performed
        public UIRightClickInputEvent() { }
    }
    
    public class UIMiddleClickInputEvent
    {
        // Middle click is typically just performed
        public UIMiddleClickInputEvent() { }
    }
    
    public class UIScrollWheelInputEvent
    {
        public Vector2 ScrollDelta { get; }
        
        public UIScrollWheelInputEvent(Vector2 scrollDelta)
        {
            ScrollDelta = scrollDelta;
        }
    }
    
    public class UITrackedDevicePositionInputEvent
    {
        public Vector3 Position { get; }
        
        public UITrackedDevicePositionInputEvent(Vector3 position)
        {
            Position = position;
        }
    }
    
    public class UITrackedDeviceOrientationInputEvent
    {
        public Quaternion Orientation { get; }
        
        public UITrackedDeviceOrientationInputEvent(Quaternion orientation)
        {
            Orientation = orientation;
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
    
    /// <summary>
    /// Event triggered when the player requests to save the game
    /// </summary>
    public class SaveGameRequestedEvent
    {
        public string SaveName { get; set; }
        public bool OverwriteExisting { get; set; }
    
        public SaveGameRequestedEvent(string saveName, bool overwriteExisting = false)
        {
            SaveName = saveName;
            OverwriteExisting = overwriteExisting;
        }
    }
    #endregion
}