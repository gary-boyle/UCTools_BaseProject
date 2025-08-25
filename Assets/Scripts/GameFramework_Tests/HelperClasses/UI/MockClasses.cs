
    
using System.Threading.Tasks;
using GameFramework.EventSystem.Interfaces;
using GameFramework.UI;
using GameFramework.UI.Interfaces;

namespace GameFramework.Tests.HelperClasses.UI
{
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GameFramework.Tests.Services
{
    public class MockUIDocumentWrapper : IUIDocumentWrapper
    {
        private VisualElement _rootElement = new VisualElement();
    
        public VisualElement RootVisualElement => _rootElement;
        public bool enabled { get; set; }
    
        public void SetRootElement(VisualElement element)
        {
            _rootElement = element;
        }
    }
    //
    // #endregion

    #region Test Helper Classes

    public class TestUIScreen : UIScreen
    {
        public TestUIScreen(VisualElement rootElement) : base(rootElement) { }
    }

    public class TestUIPopup : UIPopup
    {
        public TestUIPopup(VisualElement rootElement) : base(rootElement) { }
    }

    public class UnregisteredScreen : UIScreen
    {
        public UnregisteredScreen(VisualElement rootElement) : base(rootElement) { }
    }

    public class UnregisteredPopup : UIPopup
    {
        public UnregisteredPopup(VisualElement rootElement) : base(rootElement) { }
    }

    #endregion
}

}