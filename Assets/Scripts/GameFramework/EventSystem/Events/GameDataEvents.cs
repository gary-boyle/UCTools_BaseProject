using GameFramework.DataStructures;

namespace GameFramework.GameData.Events
{
    /// <summary>
    /// Event published when GameSessionData changes
    /// </summary>
    public class GameSessionDataChangedEvent
    {
        public GameSessionData GameSessionData { get; }

        public GameSessionDataChangedEvent(GameSessionData gameSessionData)
        {
            GameSessionData = gameSessionData;
        }
    }

    /// <summary>
    /// Event published when PlayerData changes
    /// </summary>
    public class PlayerDataChangedEvent
    {
        public PlayerData PlayerData { get; }

        public PlayerDataChangedEvent(PlayerData playerData)
        {
            PlayerData = playerData;
        }
    }

    /// <summary>
    /// Event published when a new game is started
    /// </summary>
    public class NewGameStartedEvent
    {
        public GameSessionData GameSessionData { get; }
        public PlayerData PlayerData { get; }

        public NewGameStartedEvent(GameSessionData gameSessionData, PlayerData playerData)
        {
            GameSessionData = gameSessionData;
            PlayerData = playerData;
        }
    }

    /// <summary>
    /// Event published when game data is loaded from save
    /// </summary>
    public class GameDataLoadedEvent
    {
        public GameSessionData GameSessionData { get; }
        public PlayerData PlayerData { get; }

        public GameDataLoadedEvent(GameSessionData gameSessionData, PlayerData playerData)
        {
            GameSessionData = gameSessionData;
            PlayerData = playerData;
        }
    }
}