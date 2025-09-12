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
}