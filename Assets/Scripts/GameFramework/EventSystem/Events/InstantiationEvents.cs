using System;
using UnityEngine;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Instantiation system events for game object creation and destruction
    /// Handles player instantiation and destruction during loading
    /// </summary>

    /// <summary>
    /// Event published when the player GameObject is instantiated
    /// </summary>
    public class PlayerInstantiatedEvent
    {
        public GameObject Player { get; }
        public Vector3 Position { get; }
        public Vector3 Rotation { get; }

        public PlayerInstantiatedEvent(GameObject player, Vector3 position, Vector3 rotation)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Position = position;
            Rotation = rotation;
        }
    }

    /// <summary>
    /// Event published when the player GameObject is about to be destroyed
    /// </summary>
    public class PlayerDestroyedEvent
    {
        public GameObject Player { get; }

        public PlayerDestroyedEvent(GameObject player)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
        }
    }

    /// <summary>
    /// Event published to request player instantiation during loading
    /// </summary>
    public class InstantiatePlayerEvent
    {
        public Vector3 Position { get; }
        public Vector3 Rotation { get; }
        public bool UseDefaultSettings { get; }

        public InstantiatePlayerEvent(Vector3 position, Vector3 rotation)
        {
            Position = position;
            Rotation = rotation;
            UseDefaultSettings = false;
        }

        public InstantiatePlayerEvent()
        {
            Position = Vector3.zero;
            Rotation = Vector3.zero;
            UseDefaultSettings = true;
        }
    }
}
