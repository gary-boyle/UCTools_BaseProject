using System;

namespace GameFramework.DataStructures
{
    [Serializable]
    public class PlayerData
    {
        public int level;
        public float experience;
        public int health;
        public int maxHealth;
        public float[] position = new float[3];
        public string currentScene;
    }
}