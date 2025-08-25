using System;
using GameFramework.Services.Interfaces;

namespace GameFramework.EventSystem.Interfaces
{

    /// <summary>
    /// Type-safe event system for decoupled communication between game systems.
    /// No dependencies - this is a leaf service in the dependency graph.
    /// </summary>
    public interface IEventSystem : IGameService
    {
        void Subscribe<T>(Action<T> handler) where T : class;
        void Subscribe<T>(Action handler);
        void Unsubscribe<T>(Action<T> handler) where T : class;
        void Unsubscribe<T>(Action handler);
        void Publish<T>(T eventData) where T : class;
        void Publish<T>();
        void Clear();
    }
}