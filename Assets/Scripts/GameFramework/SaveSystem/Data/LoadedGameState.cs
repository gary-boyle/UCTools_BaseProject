using GameFramework.DataStructures;

namespace GameFramework.SaveSystem.Data
{

    /// <summary>
    /// Container for converted game state objects
    /// </summary>
    public class LoadedGameState
    {
        public GameSessionData GameSessionData { get; set; }
        public PlayerSaveData PlayerSaveData { get; set; }

        public bool IsValid()
        {
            return GameSessionData != null && PlayerSaveData != null;
        }
    }
}