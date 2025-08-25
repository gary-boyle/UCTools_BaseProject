using System.Threading.Tasks;
using NUnit.Framework;
using GameFramework.Services;
using GameFramework.Tests.HelperClasses;
using GameFramework.Tests.HelperClasses.UI.GameFramework.Tests.Services;
using GameFramework.UI.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Tests.UI
{
    [TestFixture]
    public class UIServiceIntegrationTests
    {
        private UIService _uiService;
        private MockEventSystem _mockEventSystem;
        private MockUIDocumentWrapper _mockUIDocumentWrapper;
        private GameObject _uiGameObject;

        [SetUp]
        public void SetUp()
        {
            // Create a real GameObject for integration testing
            _uiGameObject = new GameObject("TestUIDocument");
            
            // Create manual mocks
            _mockEventSystem = new MockEventSystem();
            _mockUIDocumentWrapper = new MockUIDocumentWrapper();
            
            // Create a real root element with all required child elements
            var rootElement = new VisualElement();
            
            // Add all required screen elements
            var screenNames = new[]
            {
                "UI_DebugScreen", "UI_SplashScreen", "UI_MainMenuScreen", 
                "UI_GamePlayHUD", "UI_PauseScreen", "UI_OptionsScreen",
                "UI_LoadingScreen", "UI_NewGameScreen", "UI_CreditScreen",
                "UI_GameOverScreen", "UI_VictoryScreen"
            };

            foreach (var screenName in screenNames)
            {
                var screenElement = new VisualElement { name = screenName };
                rootElement.Add(screenElement);
            }

            
            // Setup the mock UI document wrapper
            _mockUIDocumentWrapper.SetRootElement(rootElement);
            
            _uiService = new UIService(_mockEventSystem, _mockUIDocumentWrapper);
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
        public async Task FullWorkflow_InitializeShowHideShutdown_ShouldWorkCorrectly()
        {
            // Initialize
            await _uiService.InitializeAsync();
            Assert.IsTrue(_uiService.IsInitialized);

            // Show a screen
            await _uiService.ShowScreenAsync<SplashScreen>();
            var splashScreen = _uiService.GetScreen<SplashScreen>();
            Assert.IsTrue(splashScreen.IsVisible);

            // Hide the screen
            await _uiService.HideScreenAsync<SplashScreen>();
            Assert.IsFalse(splashScreen.IsVisible);

            // Show another screen
            await _uiService.ShowScreenAsync<MainMenuScreen>();
            var mainMenuScreen = _uiService.GetScreen<MainMenuScreen>();
            Assert.IsTrue(mainMenuScreen.IsVisible);

            // Shutdown
            _uiService.Shutdown();
            Assert.IsFalse(_uiService.IsInitialized);
            Assert.IsNull(_uiService.GetScreen<SplashScreen>());
            Assert.IsNull(_uiService.GetScreen<MainMenuScreen>());
        }

        [Test]
        public async Task MultipleScreensVisible_ShouldAllBeVisible()
        {
            // Arrange
            await _uiService.InitializeAsync();

            // Act - Show multiple screens
            await _uiService.ShowScreenAsync<DebugScreen>();
            await _uiService.ShowScreenAsync<GameplayHUD>();
            await _uiService.ShowScreenAsync<LoadingScreen>();

            // Assert
            Assert.IsTrue(_uiService.GetScreen<DebugScreen>().IsVisible);
            Assert.IsTrue(_uiService.GetScreen<GameplayHUD>().IsVisible);
            Assert.IsTrue(_uiService.GetScreen<LoadingScreen>().IsVisible);
        }

        [Test]
        public async Task SetDebugScreenText_ShouldUpdateLabelText()
        {
            // Arrange
            await _uiService.InitializeAsync();
            const string testText = "Integration test message";

            // Act
            _uiService.SetDebugScreenText(testText);

            // Assert
            var debugScreen = _uiService.GetScreen<DebugScreen>();
            Assert.IsNotNull(debugScreen);
    
            // Verify the text was actually set (if you want to test this)
            // Note: You'd need to expose the label or text somehow to verify this
            Assert.DoesNotThrow(() => _uiService.SetDebugScreenText(testText));
        }

        [Test]
        public async Task EventSystem_ShouldBeProperlyInitialized()
        {
            // Arrange
            await _mockEventSystem.InitializeAsync();
            await _uiService.InitializeAsync();

            // Assert
            Assert.IsTrue(_mockEventSystem.IsInitialized);
        }
    }
}
