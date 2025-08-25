using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{

    /// <summary>
    /// Splash screen implementation
    /// </summary>
    public class DebugScreen : UIScreen
    {
        private VisualElement _root;
        private Label _debugLabel;
    
        public DebugScreen(VisualElement rootElement) : base(rootElement)
        {
            _root = rootElement;
            _debugLabel = rootElement?.Q<Label>("lbl_Debug");

            if (_debugLabel == null)
            {
                var debugLabel = new Label("Default Debug Text") 
                { 
                    name = "lbl_Debug" 
                };
                _root.Add(debugLabel);
            }
        }
    
        protected override void OnShow()
        {
            Debug.Log("[SplashScreen] Showing Debug screen");
        }

        public void SetText(string text)
        {
            // var debugLabel = new Label("Default Debug Text") 
            // { 
            //     name = "lbl_Debug" 
            // };
            // _root.Add(debugLabel);
            _debugLabel = _root?.Q<Label>("lbl_Debug");

            _debugLabel.text = text;
        }
    }
}