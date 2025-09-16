using System.Collections.Generic;
using GameFramework.Core;
using GameFramework.StateMachine.Enum;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Core game state and lifecycle events
    /// Handles game state transitions, pause/resume, and basic game flow
    /// </summary>
    
    public class GameStateChangeEvent
    {
        public GameStateType PreviousState { get; set; }
        public GameStateType NewState { get; set; }
        public GameContext Context { get; set; }
    }

    public class GamePausedEvent { }

    public class GameResumedEvent { }
    
    public class OptionsChangedEvent { }
    
    public class NewGameRequestedEvent
    {
        public string PlayerName { get; set; }
        public string Difficulty { get; set; } = "Normal";
        public string StartingScene { get; set; } = "GameLevel1";
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
    
}