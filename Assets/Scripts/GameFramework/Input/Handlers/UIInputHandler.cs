using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UnityEngine;

namespace GameFramework.Input.Handlers
{
    /// <summary>
    /// Handles UI input - active when any UI is visible
    /// High priority to ensure UI interactions work correctly
    /// </summary>
    public class UIInputHandler : InputHandlerBase
    {
        private readonly IUIService _uiService;
        
        public UIInputHandler(IEventSystem eventSystem, IUIService uiService, IPauseService pauseService)
            : base("UI", 800, eventSystem) // High priority
        {
            _uiService = uiService;
        }
        
        protected override void SubscribeToEvents()
        {
            _eventSystem.Subscribe<UINavigateInputEvent>(OnUINavigate);
            _eventSystem.Subscribe<UISubmitInputEvent>(OnUISubmit);
            _eventSystem.Subscribe<UICancelInputEvent>(OnUICancel);
            _eventSystem.Subscribe<UIClickInputEvent>(OnUIClick);
        }
        
        protected override void UnsubscribeFromEvents()
        {
            _eventSystem.Unsubscribe<UINavigateInputEvent>(OnUINavigate);
            _eventSystem.Unsubscribe<UISubmitInputEvent>(OnUISubmit);
            _eventSystem.Unsubscribe<UICancelInputEvent>(OnUICancel);
            _eventSystem.Unsubscribe<UIClickInputEvent>(OnUIClick);
        }
        
        public override bool HandleInput<T>(T inputEvent)
        {
            // If we have open popups, consume most input
            if (_uiService.HasOpenPopups())
            {
                return inputEvent is UINavigateInputEvent or UISubmitInputEvent or UICancelInputEvent or UIClickInputEvent;
            }
            
            return false; // Don't consume if no popups are open
        }
        
        private static void OnUINavigate(UINavigateInputEvent evt)
        {
        }
        
        private static void OnUISubmit(UISubmitInputEvent evt)
        {
        }
        
        private void OnUICancel(UICancelInputEvent evt)
        {
            // Handle cancel - context dependent
            if (_uiService.HasOpenPopups())
            {
                // Close topmost popup
                var currentPopup = _uiService.GetCurrentPopup();
                if (currentPopup != null)
                {
                    Debug.Log($"[UIInputHandler] Closing popup: {currentPopup.GetType().Name}");
                    // Close the appropriate popup type
                }
            }
            else
            {
                // No popups open - this might be a pause request
                _eventSystem.Publish(new PauseRequestedEvent());
            }
        }
        
        private static void OnUIClick(UIClickInputEvent evt)
        {
        }
    }
}
