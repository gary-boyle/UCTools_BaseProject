    using UnityEngine.UIElements;

    namespace GameFramework.UI
    {
        /// <summary>
        /// Base class for all UI popups
        /// </summary>
        public abstract class UIPopup : UIScreen
        {
            protected UIPopup(VisualElement rootElement) : base(rootElement) { }
            
            /// <summary>
            /// Whether this popup should be counted in popup management checks.
            /// Debug/utility popups should return false to avoid blocking game flow.
            /// </summary>
            public virtual bool CountsAsGameBlockingPopup => true;

        }
    }