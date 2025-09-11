using System;
using UnityEngine;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Player-specific state data
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        public int level = 1;
        public float experience = 0f;
        public int health = 100;
        public int maxHealth = 100;
        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;

        public static PlayerState CreateDefault(string difficulty)
        {
            var state = new PlayerState();

            // Adjust starting stats based on difficulty
            switch (difficulty.ToLower())
            {
                case "easy":
                    state.maxHealth = 150;
                    state.health = 150;
                    break;
                case "hard":
                    state.maxHealth = 75;
                    state.health = 75;
                    break;
                case "expert":
                    state.maxHealth = 50;
                    state.health = 50;
                    break;
                default: // Normal
                    state.maxHealth = 100;
                    state.health = 100;
                    break;
            }

            return state;
        }
    }
}
