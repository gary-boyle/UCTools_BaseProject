using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;

namespace GameFramework.UI
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Base class for all UI screens
    /// </summary>
    public abstract class UIScreen
    {
        protected VisualElement RootElement { get; private set; }
        public bool IsVisible { get; protected set; }
    
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
    
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
    }
}