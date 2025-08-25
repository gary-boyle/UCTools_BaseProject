using UnityEngine.UIElements;

namespace GameFramework.UI.Interfaces
{
    /// <summary>
    /// Minimal interface wrapper for UIDocument testing
    /// </summary>
    public interface IUIDocumentWrapper
    {
        VisualElement RootVisualElement { get; }
        bool enabled { get; set; } 
    }
}