using System.Threading.Tasks;
using NUnit.Framework;
using GameFramework.Services;
using GameFramework.Tests.HelperClasses;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Tests.UI
{
    [TestFixture]
    public class UIServiceRealUIDocumentTests
    {
        private UIService _uiService;
        private MockEventSystem _mockEventSystem;
        private GameObject _uiGameObject;
        private UIDocument _realUIDocument;

        [SetUp]
        public void SetUp()
        {
            // Create a real GameObject with UIDocument
            _uiGameObject = new GameObject("TestUIDocument");
            _realUIDocument = _uiGameObject.AddComponent<UIDocument>();
            
            // Create mock event system
            _mockEventSystem = new MockEventSystem();
        }

        [TearDown]
        public void TearDown()
        {
            _uiService?.Shutdown();
            if (_uiGameObject != null)
            {
                Object.DestroyImmediate(_uiGameObject);
            }
        }

        [Test]
        public void Constructor_WithRealUIDocument_ShouldCreateService()
        {
            // Act
            _uiService = new UIService(_mockEventSystem, _realUIDocument);

            // Assert
            Assert.IsNotNull(_uiService);
            Assert.IsFalse(_uiService.IsInitialized);
            Assert.AreEqual(_realUIDocument, _uiService.UIDocument);
        }

        [Test]
        public void UIDocumentProperty_ShouldReturnCorrectDocument()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _realUIDocument);

            // Act & Assert
            Assert.AreEqual(_realUIDocument, _uiService.UIDocument);
        }
    }
}
