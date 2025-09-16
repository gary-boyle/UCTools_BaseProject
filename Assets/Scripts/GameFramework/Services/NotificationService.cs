using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Events.Enums;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.UI.Popups;
using UnityEngine;

namespace GameFramework.Services
{
    /// <summary>
    /// Service that manages notification popups and automatically subscribes to game events
    /// to display contextual notifications to the player
    /// </summary>
    public class NotificationService : INotificationService
    {
        public bool IsInitialized { get; private set; }

        #region Dependencies
        private readonly IEventSystem _eventSystem;
        private readonly IUIService _uiService;
        #endregion

        #region Private Fields
        private NotificationPopup _notificationPopup;
        #endregion

        /// <summary>
        /// Constructor injection - receives required dependencies
        /// </summary>
        public NotificationService(IEventSystem eventSystem, IUIService uiService)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        }

        #region IGameService Implementation
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
                // Create and register the notification popup
                await InitializeNotificationPopup();
                
                // Subscribe to events that should trigger notifications
                SubscribeToGameEvents();

                IsInitialized = true;
                Debug.Log("[NotificationService] Initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NotificationService] Failed to initialize: {e}");
                throw;
            }
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            // Unsubscribe from all events
            UnsubscribeFromGameEvents();

            IsInitialized = false;
            Debug.Log("[NotificationService] Shutdown complete");
        }
        #endregion

        #region INotificationService Implementation
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f)
        {
            if (!IsInitialized || _notificationPopup == null)
            {
                Debug.LogWarning("[NotificationService] Cannot show notification - service not initialized");
                return;
            }

            _ = _notificationPopup.ShowNotificationAsync(message, type, duration);
        }

        public void ShowInfo(string message, float duration = 3f)
        {
            ShowNotification(message, NotificationType.Info, duration);
        }

        public void ShowSuccess(string message, float duration = 3f)
        {
            ShowNotification(message, NotificationType.Success, duration);
        }

        public void ShowWarning(string message, float duration = 4f)
        {
            ShowNotification(message, NotificationType.Warning, duration);
        }

        public void ShowError(string message, float duration = 5f)
        {
            ShowNotification(message, NotificationType.Error, duration);
        }

        public void HideNotification()
        {
            if (!IsInitialized || _notificationPopup == null) return;
            
            _ = _notificationPopup.HideNotificationAsync();
        }
        #endregion

        #region Private Methods
        private async Task InitializeNotificationPopup()
        {
            // Wait for UI service to be ready
            if (!_uiService.IsInitialized)
            {
                Debug.LogWarning("[NotificationService] UIService not ready, waiting...");
                // In a real scenario, you might want to wait or handle this differently
                await Task.Delay(100);
            }

            // Create the notification popup from UXML
            var uiDocument = _uiService.GetUIDocument();
            if (uiDocument?.rootVisualElement != null)
            {
                // Create a simple container directly since UXML template loading can be complex
                var notificationContainer = new UnityEngine.UIElements.VisualElement { name = "NotificationContainer" };
                notificationContainer.AddToClassList("notification-container");
                
                var panel = new UnityEngine.UIElements.VisualElement();
                panel.AddToClassList("notification-panel");
                
                var content = new UnityEngine.UIElements.VisualElement();
                content.AddToClassList("layout-row");
                
                var iconContainer = new UnityEngine.UIElements.VisualElement { name = "NotificationIcon" };
                iconContainer.AddToClassList("notification-icon");
                
                var iconLabel = new UnityEngine.UIElements.Label("ℹ") { name = "lbl_Icon" };
                iconLabel.AddToClassList("notification-icon-text");
                iconContainer.Add(iconLabel);
                
                var messageContainer = new UnityEngine.UIElements.VisualElement();
                messageContainer.AddToClassList("layout-column");
                messageContainer.AddToClassList("layout-grow");
                
                var messageLabel = new UnityEngine.UIElements.Label("Notification message goes here") { name = "lbl_Message" };
                messageLabel.AddToClassList("notification-message");
                messageLabel.AddToClassList("text");
                messageContainer.Add(messageLabel);
                
                content.Add(iconContainer);
                content.Add(messageContainer);
                panel.Add(content);
                notificationContainer.Add(panel);
                
                // Add to the root UI document
                uiDocument.rootVisualElement.Add(notificationContainer);
                
                // Create the notification popup
                _notificationPopup = new NotificationPopup(notificationContainer);
                _uiService.RegisterPopup(_notificationPopup);

                Debug.Log("[NotificationService] Notification popup created and registered");
            }
            else
            {
                throw new InvalidOperationException("UIService UIDocument is not available");
            }
        }

        private void SubscribeToGameEvents()
        {
            // Subscribe to ShowNotificationEvent for direct notification requests
            _eventSystem.Subscribe<ShowNotificationEvent>(OnShowNotificationEvent);

            // Subscribe to save/load events for contextual notifications
            _eventSystem.Subscribe<SaveCompletedEvent>(OnSaveCompleted);
            _eventSystem.Subscribe<SaveFailedEvent>(OnSaveFailed);
            _eventSystem.Subscribe<LoadingCompletedEvent>(OnLoadingCompleted);
            _eventSystem.Subscribe<LoadingFailedEvent>(OnLoadingFailed);

            // Subscribe to other game events that should show notifications
            _eventSystem.Subscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem.Subscribe<GameResumedEvent>(OnGameResumed);
            _eventSystem.Subscribe<SceneLoadedEvent>(OnSceneLoaded);

            Debug.Log("[NotificationService] Subscribed to game events");
        }

        private void UnsubscribeFromGameEvents()
        {
            _eventSystem.Unsubscribe<ShowNotificationEvent>(OnShowNotificationEvent);
            _eventSystem.Unsubscribe<SaveCompletedEvent>(OnSaveCompleted);
            _eventSystem.Unsubscribe<SaveFailedEvent>(OnSaveFailed);
            _eventSystem.Unsubscribe<LoadingCompletedEvent>(OnLoadingCompleted);
            _eventSystem.Unsubscribe<LoadingFailedEvent>(OnLoadingFailed);
            _eventSystem.Unsubscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem.Unsubscribe<GameResumedEvent>(OnGameResumed);
            _eventSystem.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);

            Debug.Log("[NotificationService] Unsubscribed from game events");
        }
        #endregion

        #region Event Handlers
        private void OnShowNotificationEvent(ShowNotificationEvent eventData)
        {
            ShowNotification(eventData.Message, eventData.Type, eventData.Duration);
        }

        private void OnSaveCompleted(SaveCompletedEvent eventData)
        {
            var message = eventData.IsAutoSave ? "Auto-save completed" : "Game saved successfully";
            ShowSuccess(message, 2f);
        }

        private void OnSaveFailed(SaveFailedEvent eventData)
        {
            var message = $"Save failed: {eventData.ErrorMessage}";
            ShowError(message, 4f);
        }

        private void OnLoadingCompleted(LoadingCompletedEvent eventData)
        {
            ShowSuccess("Game loaded successfully", 2f);
        }

        private void OnLoadingFailed(LoadingFailedEvent eventData)
        {
            ShowError($"Load failed: {eventData.Exception?.Message ?? "Unknown error"}", 4f);
        }

        private void OnGamePaused(GamePausedEvent eventData)
        {
            ShowInfo("Game Paused", 1.5f);
        }

        private void OnGameResumed(GameResumedEvent eventData)
        {
            ShowInfo("Game Resumed", 1.5f);
        }

        private void OnSceneLoaded(SceneLoadedEvent eventData)
        {
            ShowInfo($"Entered {eventData.SceneName}", 2f);
        }
        #endregion
    }
}