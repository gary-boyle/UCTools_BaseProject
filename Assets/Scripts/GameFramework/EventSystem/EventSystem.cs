using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;

namespace GameFramework.EventSystem
{
    public class EventSystem : IEventSystem
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        public bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Constructor injection - EventSystem has no dependencies
        /// </summary>
        public EventSystem()
        {
            // No dependencies to inject
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            IsInitialized = true;
            await Task.CompletedTask;
        }
        
        public void Shutdown()
        {
            Clear();
            IsInitialized = false;
        }
        
        public void Subscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType))
                _handlers[eventType] = new List<Delegate>();
                
            _handlers[eventType].Add(handler);
        }
        
        public void Subscribe<T>(Action handler)
        {
            var eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType))
                _handlers[eventType] = new List<Delegate>();
                
            _handlers[eventType].Add(handler);
        }
        
        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            var eventType = typeof(T);
            if (!_handlers.TryGetValue(eventType, out var handlers)) return;
            
            handlers.Remove(handler);
            if (handlers.Count == 0)
                _handlers.Remove(eventType);
        }
        
        public void Unsubscribe<T>(Action handler)
        {
            var eventType = typeof(T);
            if (!_handlers.TryGetValue(eventType, out var handlers)) return;
            
            handlers.Remove(handler);
            if (handlers.Count == 0)
                _handlers.Remove(eventType);
        }
        
        public void Publish<T>(T eventData) where T : class
        {
            var eventType = typeof(T);
            if (!_handlers.TryGetValue(eventType, out var handlers)) return;
            
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (handlers[i] is Action<T> handler)
                        handler.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventSystem] Error in event handler for {eventType.Name}: {e}");
                }
            }
        }
        
        public void Publish<T>()
        {
            var eventType = typeof(T);
            if (!_handlers.TryGetValue(eventType, out var handlers)) return;
            
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (handlers[i] is Action handler)
                        handler.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventSystem] Error in event handler for {eventType.Name}: {e}");
                }
            }
        }
        
        public void Clear()
        {
            _handlers.Clear();
        }
    }
}