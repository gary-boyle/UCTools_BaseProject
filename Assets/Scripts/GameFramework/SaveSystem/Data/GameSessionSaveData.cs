namespace GameFramework.SaveSystem.Data
{
    [System.Serializable]
    public class GameSessionSaveData
    {
        public string uniqueID;
        public string difficulty;
        public string currentScene;
        public long gameTime;
    }
}