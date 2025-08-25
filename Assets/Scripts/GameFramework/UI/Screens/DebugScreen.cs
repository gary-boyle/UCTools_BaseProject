using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{

    /// <summary>
    /// Splash screen implementation
    /// </summary>
    public class DebugScreen : UIScreen
    {
        private Label _debugLabel;
    
        public DebugScreen(VisualElement rootElement) : base(rootElement)
        {
            _debugLabel = rootElement?.Q<Label>("lbl_Debug");
        }
    
        protected override void OnShow()
        {
            Debug.Log("[SplashScreen] Showing Debug screen");
        }

        public void SetText(string text)
        {
            _debugLabel.text = text;
        }
    }
}