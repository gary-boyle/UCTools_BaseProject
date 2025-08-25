using System;
using System.Threading.Tasks;
using NUnit.Framework;
using GameFramework.Services;
using GameFramework.UI.Interfaces;
using GameFramework.UI.Screens;
using GameFramework.Tests.HelperClasses;
using GameFramework.Tests.HelperClasses.UI.GameFramework.Tests.Services;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameFramework.Tests.UI
{
    [TestFixture]
    public class UIServiceTests
    {
        private UIService _uiService;
        private MockEventSystem _mockEventSystem;
        private MockUIDocumentWrapper _mockUIDocumentWrapper;
        private VisualElement _mockRootElement;
        private GameObject _testGameObject;
        private UIDocument _realUIDocument;

        [SetUp]
        public void SetUp()
        {
            // Create manual mocks
            _mockEventSystem = new MockEventSystem();
            _mockUIDocumentWrapper = new MockUIDocumentWrapper();
            _mockRootElement = new VisualElement();

            // Setup mock UI document wrapper
            _mockUIDocumentWrapper.SetRootElement(_mockRootElement);
            
            // Create real UIDocument for tests that need it
            _testGameObject = new GameObject("TestUIDocument");
            _realUIDocument = _testGameObject.AddComponent<UIDocument>();
            
            // Create mock visual elements for all screens
            SetupMockVisualElements();
        }

        private void SetupMockVisualElements()
        {
            var screens = new[]
            {
                "UI_DebugScreen", "UI_SplashScreen", "UI_MainMenuScreen", 
                "UI_GamePlayHUD", "UI_PauseScreen", "UI_OptionsScreen",
                "UI_LoadingScreen", "UI_NewGameScreen", "UI_CreditScreen",
                "UI_GameOverScreen", "UI_VictoryScreen"
            };

            foreach (var screenName in screens)
            {
                var mockElement = new VisualElement { name = screenName };
        
                // Add the specific Label that DebugScreen expects
                if (screenName == "UI_DebugScreen")
                {
                    var debugLabel = new Label("Default Debug Text") 
                    { 
                        name = "lbl_Debug" 
                    };
                    mockElement.Add(debugLabel);
                }
        
                _mockRootElement.Add(mockElement);
            }
        }

        [TearDown]
        public void TearDown()
        {
            _uiService?.Shutdown();
            _uiService = null;
            
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidUIDocumentWrapper_ShouldCreateService()
        {
            // Act
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);

            // Assert
            Assert.IsNotNull(_uiService);
            Assert.IsFalse(_uiService.IsInitialized);
        }

        [Test]
        public void Constructor_WithValidUIDocument_ShouldCreateService()
        {
            // Act
            _uiService = new UIService(_mockEventSystem, _realUIDocument);

            // Assert
            Assert.IsNotNull(_uiService);
            Assert.IsFalse(_uiService.IsInitialized);
            Assert.AreEqual(_realUIDocument, _uiService.UIDocument);
        }

        [Test]
        public void Constructor_WithNullEventSystem_UIDocumentWrapper_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new UIService(null, _mockUIDocumentWrapper));
        }

        [Test]
        public void Constructor_WithNullEventSystem_UIDocument_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new UIService(null, _realUIDocument));
        }

        [Test]
        public void Constructor_WithNullUIDocument_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new UIService(_mockEventSystem, (UIDocument)null));
        }

        [Test]
        public void Constructor_WithNullUIDocumentWrapper_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new UIService(_mockEventSystem, (IUIDocumentWrapper)null));
        }

        #endregion

        #region Initialization Tests

        [Test]
        public async Task InitializeAsync_WithUIDocumentWrapper_ShouldSetIsInitializedToTrue()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);

            // Act
            await _uiService.InitializeAsync();

            // Assert
            Assert.IsTrue(_uiService.IsInitialized);
        }

        [Test]
        public async Task InitializeAsync_CalledMultipleTimes_ShouldOnlyInitializeOnce()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            
            // Act
            await _uiService.InitializeAsync();
            await _uiService.InitializeAsync();
            await _uiService.InitializeAsync();

            // Assert
            Assert.IsTrue(_uiService.IsInitialized);
        }

        [Test]
        public async Task InitializeAsync_ShouldRegisterAllScreens()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);

            // Act
            await _uiService.InitializeAsync();

            // Assert - Check that screens are registered by trying to get them
            Assert.IsNotNull(_uiService.GetScreen<DebugScreen>());
            Assert.IsNotNull(_uiService.GetScreen<SplashScreen>());
            Assert.IsNotNull(_uiService.GetScreen<MainMenuScreen>());
            Assert.IsNotNull(_uiService.GetScreen<GameplayHUD>());
            Assert.IsNotNull(_uiService.GetScreen<PauseScreen>());
            Assert.IsNotNull(_uiService.GetScreen<OptionsScreen>());
            Assert.IsNotNull(_uiService.GetScreen<LoadingScreen>());
            Assert.IsNotNull(_uiService.GetScreen<NewGameScreen>());
            Assert.IsNotNull(_uiService.GetScreen<CreditsScreen>());
            Assert.IsNotNull(_uiService.GetScreen<GameOverScreen>());
            Assert.IsNotNull(_uiService.GetScreen<VictoryScreen>());
        }

        #endregion

        #region Shutdown Tests

        [Test]
        public async Task Shutdown_ShouldSetIsInitializedToFalse()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            await _uiService.InitializeAsync();

            // Act
            _uiService.Shutdown();

            // Assert
            Assert.IsFalse(_uiService.IsInitialized);
        }

        [Test]
        public async Task Shutdown_ShouldClearAllScreens()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            await _uiService.InitializeAsync();

            // Act
            _uiService.Shutdown();

            // Assert
            Assert.IsNull(_uiService.GetScreen<DebugScreen>());
            Assert.IsNull(_uiService.GetScreen<SplashScreen>());
            Assert.IsNull(_uiService.GetScreen<MainMenuScreen>());
        }

        #endregion

        #region Screen Management Tests

        [Test]
        public void RegisterScreen_ShouldAddScreenToCollection()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            var mockScreen = new TestUIScreen(new VisualElement());

            // Act
            _uiService.RegisterScreen(mockScreen);

            // Assert
            Assert.AreEqual(mockScreen, _uiService.GetScreen<TestUIScreen>());
        }

        [Test]
        public async Task ShowScreenAsync_WithRegisteredScreen_ShouldShowScreen()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            await _uiService.InitializeAsync();

            // Act
            await _uiService.ShowScreenAsync<DebugScreen>();

            // Assert
            var screen = _uiService.GetScreen<DebugScreen>();
            Assert.IsTrue(screen.IsVisible);
        }

        [Test]
        public async Task HideScreenAsync_WithRegisteredScreen_ShouldHideScreen()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            await _uiService.InitializeAsync();
            await _uiService.ShowScreenAsync<DebugScreen>();

            // Act
            await _uiService.HideScreenAsync<DebugScreen>();

            // Assert
            var screen = _uiService.GetScreen<DebugScreen>();
            Assert.IsFalse(screen.IsVisible);
        }

        [Test]
        public async Task ShowScreenAsync_WithUnregisteredScreen_ShouldLogError()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            LogAssert.Expect(LogType.Error, 
                "[UIService] Screen of type UnregisteredScreen not registered");

            // Act
            await _uiService.ShowScreenAsync<UnregisteredScreen>();

            // Assert is handled by LogAssert.Expect
        }

        [Test]
        public async Task HideScreenAsync_WithUnregisteredScreen_ShouldLogError()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            LogAssert.Expect(LogType.Error, 
                "[UIService] Screen of type UnregisteredScreen not registered");

            // Act
            await _uiService.HideScreenAsync<UnregisteredScreen>();

            // Assert is handled by LogAssert.Expect
        }

        #endregion

        #region Popup Management Tests

        [Test]
        public void RegisterPopup_ShouldAddPopupToCollection()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            var mockPopup = new TestUIPopup(new VisualElement());

            // Act
            _uiService.RegisterPopup(mockPopup);

            // Assert
            Assert.AreEqual(mockPopup, _uiService.GetPopup<TestUIPopup>());
        }

        [Test]
        public async Task ShowPopupAsync_WithRegisteredPopup_ShouldShowPopup()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            var mockPopup = new TestUIPopup(new VisualElement());
            _uiService.RegisterPopup(mockPopup);

            // Act
            await _uiService.ShowPopupAsync<TestUIPopup>();

            // Assert
            Assert.IsTrue(mockPopup.IsVisible);
        }

        [Test]
        public async Task HidePopupAsync_WithRegisteredPopup_ShouldHidePopup()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            var mockPopup = new TestUIPopup(new VisualElement());
            _uiService.RegisterPopup(mockPopup);
            await _uiService.ShowPopupAsync<TestUIPopup>();

            // Act
            await _uiService.HidePopupAsync<TestUIPopup>();

            // Assert
            Assert.IsFalse(mockPopup.IsVisible);
        }

        [Test]
        public async Task ShowPopupAsync_WithUnregisteredPopup_ShouldLogError()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            LogAssert.Expect(LogType.Error, 
                "[UIService] Popup of type UnregisteredPopup not registered");

            // Act
            await _uiService.ShowPopupAsync<UnregisteredPopup>();

            // Assert is handled by LogAssert.Expect
        }

        #endregion

        #region Debug Screen Tests

        [Test]
        public async Task SetDebugScreenText_ShouldCallSetTextOnDebugScreen()
        {
            // Arrange
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
            await _uiService.InitializeAsync();
            const string testText = "Test debug text";

            // Act & Assert - Verify no exception is thrown
            Assert.DoesNotThrow(() => _uiService.SetDebugScreenText(testText));
        }

        #endregion
    }
}
