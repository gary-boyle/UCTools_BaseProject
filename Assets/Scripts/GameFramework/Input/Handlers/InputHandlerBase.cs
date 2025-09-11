using System;
using GameFramework.EventSystem.Interfaces;
using UnityEngine;

namespace GameFramework.Input.Handlers
{
    /// <summary>
    /// Base class for all input handlers
    /// Each handler is responsible for one specific type of input (UI, Player, Console, etc.)
    /// </summary>
    public abstract class InputHandlerBase
    {
        private string Name { get; set; }
        public int Priority { get; protected set; } // Higher = more important
        private bool IsActive { get; set; }
        private bool ConsumeInput { get; set; } // If true, stops event propagation to lower priority handlers
        
        protected readonly IEventSystem _eventSystem;
        
        protected InputHandlerBase(string name, int priority, IEventSystem eventSystem, bool consumeInput = true)
        {
            Name = name;
            Priority = priority;
            ConsumeInput = consumeInput;
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
        }
        
        public void Activate()
        {
            if (IsActive) return;
            
            IsActive = true;
            OnActivate();
            SubscribeToEvents();
            Debug.Log($"[InputHandler] {Name} activated (Priority: {Priority})");
        }
        
        public void Deactivate()
        {
            if (!IsActive) return;
            
            IsActive = false;
            UnsubscribeFromEvents();
            OnDeactivate();
            Debug.Log($"[InputHandler] {Name} deactivated");
        }
        
        /// <summary>
        /// Handle an input event. Return true if the event was consumed and should not propagate to lower priority handlers.
        /// </summary>
        public abstract bool HandleInput<T>(T inputEvent) where T : class;
        
        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected abstract void SubscribeToEvents();
        protected abstract void UnsubscribeFromEvents();
    }
}