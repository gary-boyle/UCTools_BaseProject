using UnityEngine.UIElements;

namespace GameFramework.UI
{
    /// <summary>
    /// Base class for all UI popups
    /// </summary>
    public abstract class UIPopup : UIScreen
    {
        protected UIPopup(VisualElement rootElement) : base(rootElement) { }
    }
}