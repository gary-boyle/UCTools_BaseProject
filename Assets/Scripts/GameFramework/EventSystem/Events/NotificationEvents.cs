using System;
using GameFramework.EventSystem.Events.Enums;

namespace GameFramework.EventSystem.Events
{
    /// <summary>
    /// Notification system events for displaying temporary UI messages
    /// These events trigger notification popups that appear in the top-left corner
    /// </summary>
    
    /// <summary>
    /// Event to show a notification popup with a message
    /// </summary>
    public class ShowNotificationEvent
    {
        public string Message { get; }
        public NotificationType Type { get; }
        public float Duration { get; }

        public ShowNotificationEvent(string message, NotificationType type = NotificationType.Info, float duration = 3f)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Type = type;
            Duration = Math.Max(0.1f, duration); // Ensure minimum duration
        }

        /// <summary>
        /// Creates an info notification
        /// </summary>
        public static ShowNotificationEvent CreateInfo(string message, float duration = 3f) 
            => new ShowNotificationEvent(message, NotificationType.Info, duration);

        /// <summary>
        /// Creates a success notification
        /// </summary>
        public static ShowNotificationEvent CreateSuccess(string message, float duration = 3f) 
            => new ShowNotificationEvent(message, NotificationType.Success, duration);

        /// <summary>
        /// Creates a warning notification
        /// </summary>
        public static ShowNotificationEvent CreateWarning(string message, float duration = 4f) 
            => new ShowNotificationEvent(message, NotificationType.Warning, duration);

        /// <summary>
        /// Creates an error notification
        /// </summary>
        public static ShowNotificationEvent CreateError(string message, float duration = 5f) 
            => new ShowNotificationEvent(message, NotificationType.Error, duration);
    }

}