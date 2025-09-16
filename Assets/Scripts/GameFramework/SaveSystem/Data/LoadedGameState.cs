using GameFramework.DataStructures;

namespace GameFramework.SaveSystem.Data
{

    /// <summary>
    /// Container for converted game state objects
    /// </summary>
    public class LoadedGameState
    {
        public GameSessionData GameSessionData { get; set; }
        public PlayerData PlayerData { get; set; }

        public bool IsValid()
        {
            return GameSessionData != null && PlayerData != null;
        }
    }
}