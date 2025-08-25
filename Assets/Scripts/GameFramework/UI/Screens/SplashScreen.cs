using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{

    /// <summary>
    /// Splash screen implementation
    /// </summary>
    public class SplashScreen : UIScreen
    {
        private Label _versionLabel;
    
        public SplashScreen(VisualElement rootElement) : base(rootElement)
        {
            _versionLabel = rootElement?.Q<Label>("VersionLabel");
            if (_versionLabel != null)
            {
                _versionLabel.text = $"Version {Application.version}";
            }
        }
    
        protected override void OnShow()
        {
            // Add any splash screen specific logic
            Debug.Log("[SplashScreen] Showing splash screen");
        }
    }
}