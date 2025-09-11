using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameFramework.DataStructures
{
    /// <summary>
    /// Player-specific state data
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        public int Level = 1;
        public float Experience;
        public int Health = 100;
        public int MaxHealth = 100;
        public Vector3 Position = Vector3.zero;
        public Vector3 Rotation = Vector3.zero;

        public static PlayerState CreateDefault(string difficulty)
        {
            var state = new PlayerState();

            // Adjust starting stats based on difficulty
            switch (difficulty.ToLower())
            {
                case "easy":
                    state.MaxHealth = 150;
                    state.Health = 150;
                    break;
                case "hard":
                    state.MaxHealth = 75;
                    state.Health = 75;
                    break;
                case "expert":
                    state.MaxHealth = 50;
                    state.Health = 50;
                    break;
                default: // Normal
                    state.MaxHealth = 100;
                    state.Health = 100;
                    break;
            }

            return state;
        }
    }
}
