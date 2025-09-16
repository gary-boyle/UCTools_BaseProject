using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Events.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Popups
{
    /// <summary>
    /// Notification popup that displays temporary messages in the top-left corner of the screen
    /// Auto-dismisses after a specified duration with smooth fade in/out animations
    /// </summary>
    public class NotificationPopup : UIPopup
    {
        #region UI Elements
        private VisualElement _notificationPanel;
        private VisualElement _notificationIcon;
        private Label _iconLabel;
        private Label _messageLabel;
        #endregion

        #region Private Fields
        private float _displayDuration = 3f;
        private float _currentTimer = 0f;
        private bool _isVisible = false;
        private bool _isAnimating = false;
        #endregion

        #region Constants
        private const string ICON_INFO = "ℹ";
        private const string ICON_SUCCESS = "✓";
        private const string ICON_WARNING = "⚠";
        private const string ICON_ERROR = "✕";
        
        private const string PANEL_SUCCESS_CLASS = "notification-panel--success";
        private const string PANEL_WARNING_CLASS = "notification-panel--warning";
        private const string PANEL_ERROR_CLASS = "notification-panel--error";
        private const string PANEL_VISIBLE_CLASS = "notification-panel--visible";
        
        private const string ICON_SUCCESS_CLASS = "notification-icon--success";
        private const string ICON_WARNING_CLASS = "notification-icon--warning";
        private const string ICON_ERROR_CLASS = "notification-icon--error";
        #endregion

        /// <summary>
        /// This popup should not block game flow - it's just an overlay notification
        /// </summary>
        public override bool CountsAsGameBlockingPopup => false;

        public NotificationPopup(VisualElement rootElement) : base(rootElement)
        {
            InitializeUI();
            EnableFrameUpdates();
            Hide(); // Start hidden
        }

        #region Initialization
        private void InitializeUI()
        {
            var container = RootElement.Q<VisualElement>("NotificationContainer");
            if (container != null)
            {
                _notificationPanel = container.Q<VisualElement>(className: "notification-panel");
            }
            
            _notificationIcon = RootElement.Q<VisualElement>("NotificationIcon");
            _iconLabel = RootElement.Q<Label>("lbl_Icon");
            _messageLabel = RootElement.Q<Label>("lbl_Message");

            if (_notificationPanel == null)
            {
                Debug.LogError("[NotificationPopup] Could not find notification panel element");
                return;
            }

            // Ensure the panel starts invisible
            _notificationPanel.RemoveFromClassList(PANEL_VISIBLE_CLASS);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Shows the notification with specified message, type, and duration
        /// </summary>
        public async Task ShowNotificationAsync(string message, NotificationType type = NotificationType.Info, float duration = 3f)
        {
            if (_isAnimating) return; // Prevent multiple animations

            _displayDuration = duration;
            _currentTimer = 0f;

            SetNotificationContent(message, type);
            await ShowWithAnimation();
        }

        /// <summary>
        /// Immediately hides the notification
        /// </summary>
        public async Task HideNotificationAsync()
        {
            if (!_isVisible) return;
            
            await HideWithAnimation();
        }
        #endregion

        #region Private Methods
        private void SetNotificationContent(string message, NotificationType type)
        {
            if (_messageLabel != null)
                _messageLabel.text = message;

            // Reset all type-specific classes
            _notificationPanel?.RemoveFromClassList(PANEL_SUCCESS_CLASS);
            _notificationPanel?.RemoveFromClassList(PANEL_WARNING_CLASS);
            _notificationPanel?.RemoveFromClassList(PANEL_ERROR_CLASS);
            
            _notificationIcon?.RemoveFromClassList(ICON_SUCCESS_CLASS);
            _notificationIcon?.RemoveFromClassList(ICON_WARNING_CLASS);
            _notificationIcon?.RemoveFromClassList(ICON_ERROR_CLASS);

            // Apply type-specific styling and icon
            switch (type)
            {
                case NotificationType.Success:
                    _notificationPanel?.AddToClassList(PANEL_SUCCESS_CLASS);
                    _notificationIcon?.AddToClassList(ICON_SUCCESS_CLASS);
                    if (_iconLabel != null) _iconLabel.text = ICON_SUCCESS;
                    break;
                case NotificationType.Warning:
                    _notificationPanel?.AddToClassList(PANEL_WARNING_CLASS);
                    _notificationIcon?.AddToClassList(ICON_WARNING_CLASS);
                    if (_iconLabel != null) _iconLabel.text = ICON_WARNING;
                    break;
                case NotificationType.Error:
                    _notificationPanel?.AddToClassList(PANEL_ERROR_CLASS);
                    _notificationIcon?.AddToClassList(ICON_ERROR_CLASS);
                    if (_iconLabel != null) _iconLabel.text = ICON_ERROR;
                    break;
                case NotificationType.Info:
                default:
                    if (_iconLabel != null) _iconLabel.text = ICON_INFO;
                    break;
            }
        }

        private async Task ShowWithAnimation()
        {
            _isAnimating = true;
            
            // Show the popup element
            Show();
            
            // Trigger the CSS animation by adding the visible class
            _notificationPanel?.AddToClassList(PANEL_VISIBLE_CLASS);
            
            // Wait for animation to complete (CSS transition is 0.3s)
            await Task.Delay(300);
            
            _isVisible = true;
            _isAnimating = false;
        }

        private async Task HideWithAnimation()
        {
            _isAnimating = true;
            
            // Trigger the CSS animation by removing the visible class
            _notificationPanel?.RemoveFromClassList(PANEL_VISIBLE_CLASS);
            
            // Wait for animation to complete (CSS transition is 0.3s)
            await Task.Delay(300);
            
            // Hide the popup element
            Hide();
            
            _isVisible = false;
            _isAnimating = false;
        }
        #endregion

        #region Frame Updates
        protected override void OnUpdate(float deltaTime)
        {
            if (!_isVisible || _isAnimating) return;

            _currentTimer += deltaTime;
            
            if (_currentTimer >= _displayDuration)
            {
                // Auto-dismiss the notification
                _ = HideNotificationAsync();
            }
        }

        /// <summary>
        /// Notification should update even when the game is paused
        /// </summary>
        public override bool ShouldUpdateWhenPaused()
        {
            return true;
        }
        #endregion
    }
}