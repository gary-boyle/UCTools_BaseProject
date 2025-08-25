namespace GameFramework.DataStructures
{
    using System;

    /// <summary>
    /// Save data structure
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string timestamp;
        public PlayerData playerData;
        public GameStateData gameStateData;
    }
}