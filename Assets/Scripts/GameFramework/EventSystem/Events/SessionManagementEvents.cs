using System;
using GameFramework.Core;
using GameFramework.DataStructures;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Session lifecycle events for game session management
    /// Handles session creation, loading, and cleanup
    /// </summary>

    public class SessionCreatedEvent
    {
        public GameSessionData SessionData { get; }
    
        public SessionCreatedEvent(GameSessionData sessionData)
        {
            SessionData = sessionData ?? throw new ArgumentNullException(nameof(sessionData));
        }
    }

    public class SessionLoadedEvent
    {
        public GameSessionData SessionData { get; }
    
        public SessionLoadedEvent(GameSessionData sessionData)
        {
            SessionData = sessionData ?? throw new ArgumentNullException(nameof(sessionData));
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
}