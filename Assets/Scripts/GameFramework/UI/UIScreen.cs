using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI
{
    /// <summary>
    /// Base class for all UI screens with optional frame update support
    /// Frame updates are managed centrally by UIService for performance
    /// </summary>
    public abstract class UIScreen
    {
        protected VisualElement RootElement { get; private set; }
        public bool IsVisible { get; protected set; }
        public bool NeedsFrameUpdates { get; private set; }

        protected IEventSystem _eventSystem;

        protected UIScreen(VisualElement rootElement)
        {
            RootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));
            _eventSystem = GameManager.GetService<IEventSystem>() ?? throw new ArgumentNullException(nameof(_eventSystem));
            
            Hide(); // Start hidden
        }

        public virtual void Show()
        {
            RootElement.style.display = DisplayStyle.Flex;
            IsVisible = true;
            OnShow();
        }

        public virtual void Hide()
        {
            RootElement.style.display = DisplayStyle.None;
            IsVisible = false;
            OnHide();
        }

        /// <summary>
        /// Call this in derived constructors if the screen needs frame updates
        /// This will register the screen with UIService for centralized updates
        /// </summary>
        protected void EnableFrameUpdates()
        {
            if (!NeedsFrameUpdates)
            {
                NeedsFrameUpdates = true;
                
                // Register with UIService for updates
                var uiService = GameManager.GetService<Services.UIService>();
                uiService?.RegisterScreenForUpdates(this);
            }
        }

        /// <summary>
        /// Disable frame updates for this screen
        /// </summary>
        protected void DisableFrameUpdates()
        {
            if (NeedsFrameUpdates)
            {
                NeedsFrameUpdates = false;
                
                // Unregister from UIService updates
                var uiService = GameManager.GetService<Services.UIService>();
                uiService?.UnregisterScreenFromUpdates(this);
            }
        }

        /// <summary>
        /// Override this for frame-based updates (only called when screen is visible and needs updates)
        /// </summary>
        protected virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// Internal update method called by UIService
        /// DO NOT CALL DIRECTLY - this is managed by UIService
        /// </summary>
        internal void InternalUpdate(float deltaTime)
        {
            if (IsVisible && NeedsFrameUpdates)
            {
                OnUpdate(deltaTime);
            }
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        
        /// <summary>
        /// Clean up when screen is destroyed
        /// </summary>
        public virtual void Cleanup()
        {
            if (NeedsFrameUpdates)
            {
                var uiService = GameManager.GetService<Services.UIService>();
                uiService?.UnregisterScreenFromUpdates(this);
            }
        }
    }
}
