// // Tests/UIServiceTests.cs
// using System;
// using System.Threading.Tasks;
// using NUnit.Framework;
// using GameFramework.Services;
// using GameFramework.UI.Screens;
// using UnityEngine;
// using UnityEngine.UIElements;
// using UnityEngine.TestTools;
// using Moq;
//
// namespace GameFramework.Tests
// {
//     /// <summary>
//     /// Comprehensive test suite for UIService class.
//     /// Tests UI lifecycle management, screen/popup handling, and dependency injection.
//     /// 
//     /// Design: Mock-based testing with Unity UI element simulation
//     /// Pros: Isolated testing, dependency validation, comprehensive coverage
//     /// Cons: Complex mock setup, UI element testing limitations in unit tests
//     /// </summary>
//     public class UIServiceTests
//     {
//         private UIService _uiService;
//         private MockEventSystem _mockEventSystem;
//         private Mock<UIDocument> _mockUIDocument;
//         private Mock<VisualElement> _mockRootElement;
//
//         [SetUp]
//         public void Setup()
//         {
//             // Create mock dependencies
//             _mockEventSystem = new MockEventSystem();
//             _mockUIDocument = new Mock<UIDocument>();
//             _mockRootElement = new Mock<VisualElement>();
//
//             // Setup mock UIDocument to return mock root element
//             _mockUIDocument.Setup(x => x.rootVisualElement).Returns(_mockRootElement.Object);
//
//             // Setup mock root element to return mock child elements for UI queries
//             var mockDebugScreen = new Mock<VisualElement>();
//             _mockRootElement.Setup(x => x.Q<VisualElement>("UI_DebugScreen"))
//                           .Returns(mockDebugScreen.Object);
//
//             // Create UIService with mocked dependencies
//             _uiService = new UIService(_mockEventSystem, _mockUIDocument.Object);
//         }
//
//         [TearDown]
//         public void TearDown()
//         {
//             _uiService?.Shutdown();
//         }
//
//         #region Constructor Tests
//
//         [Test]
//         public void Constructor_WithNullEventSystem_ShouldThrowArgumentNullException()
//         {
//             // Act & Assert
//             Assert.Throws<ArgumentNullException>(() => 
//                 new UIService(null, _mockUIDocument.Object),
//                 "Should throw ArgumentNullException for null event system");
//         }
//
//         [Test]
//         public void Constructor_WithNullUIDocument_ShouldThrowArgumentNullException()
//         {
//             // Act & Assert
//             Assert.Throws<ArgumentNullException>(() => 
//                 new UIService(_mockEventSystem, null),
//                 "Should throw ArgumentNullException for null UI document");
//         }
//
//         [Test]
//         public void Constructor_WithValidParameters_ShouldSetProperties()
//         {
//             // Act & Assert
//             Assert.AreEqual(_mockUIDocument.Object, _uiService.UIDocument, 
//                 "UIDocument property should be set correctly");
//             Assert.IsFalse(_uiService.IsInitialized, 
//                 "Should not be initialized on construction");
//         }
//
//         #endregion
//
//         #region Initialization Tests
//
//         [Test]
//         public async Task InitializeAsync_ShouldSetInitializedFlag()
//         {
//             // Arrange
//             SetupMockUIElementsForInitialization();
//
//             // Act
//             await _uiService.InitializeAsync();
//
//             // Assert
//             Assert.IsTrue(_uiService.IsInitialized, "Should be initialized after InitializeAsync");
//         }
//
//         [Test]
//         public async Task InitializeAsync_CalledTwice_ShouldNotReinitialize()
//         {
//             // Arrange
//             SetupMockUIElementsForInitialization();
//             await _uiService.InitializeAsync();
//             var wasInitialized = _uiService.IsInitialized;
//
//             // Act
//             await _uiService.InitializeAsync(); // Second call
//
//             // Assert
//             Assert.IsTrue(wasInitialized, "Should remain initialized");
//             Assert.IsTrue(_uiService.IsInitialized, "Should still be initialized");
//         }
//
//         [Test]
//         public void Shutdown_ShouldResetInitializationFlag()
//         {
//             // Arrange
//             SetupMockUIElementsForInitialization();
//             _uiService.InitializeAsync();
//
//             // Act
//             _uiService.Shutdown();
//
//             // Assert
//             Assert.IsFalse(_uiService.IsInitialized, "Should not be initialized after shutdown");
//         }
//
//         #endregion
//
//         #region Screen Management Tests
//
//         [Test]
//         public void RegisterScreen_ShouldAddScreenToCollection()
//         {
//             // Arrange
//             var mockScreen = new Mock<DebugScreen>(Mock.Of<VisualElement>());
//             mockScreen.CallBase = true;
//
//             // Act
//             _uiService.RegisterScreen(mockScreen.Object);
//             var retrievedScreen = _uiService.GetScreen<DebugScreen>();
//
//             // Assert
//             Assert.AreEqual(mockScreen.Object, retrievedScreen, "Registered screen should be retrievable");
//         }
//
//         [Test]
//         public void GetScreen_UnregisteredType_ShouldReturnNull()
//         {
//             // Act
//             var screen = _uiService.GetScreen<DebugScreen>();
//
//             // Assert
//             Assert.IsNull(screen, "Should return null for unregistered screen type");
//         }
//
//         [Test]
//         public async Task ShowScreenAsync_RegisteredScreen_ShouldCallShow()
//         {
//             // Arrange
//             var mockScreen = new Mock<DebugScreen>(Mock.Of<VisualElement>());
//             mockScreen.CallBase = true;
//             _uiService.RegisterScreen(mockScreen.Object);
//
//             // Act
//             await _uiService.ShowScreenAsync<DebugScreen>();
//
//             // Assert
//             mockScreen.Verify(x => x.Show(), Times.Once, "Show should be called on the screen");
//         }
//
//         [Test]
//         public async Task ShowScreenAsync_UnregisteredScreen_ShouldLogError()
//         {
//             // Act & Assert
//             LogAssert.Expect(LogType.Error, 
//                 new System.Text.RegularExpressions.Regex(".*Screen of type.*not registered.*"));
//             
//             await _uiService.ShowScreenAsync<DebugScreen>();
//         }
//
//         [Test]
//         public async Task HideScreenAsync_RegisteredScreen_ShouldCallHide()
//         {
//             // Arrange
//             var mockScreen = new Mock<DebugScreen>(Mock.Of<VisualElement>());
//             mockScreen.CallBase = true;
//             _uiService.RegisterScreen(mockScreen.Object);
//
//             // Act
//             await _uiService.HideScreenAsync<DebugScreen>();
//
//             // Assert
//             mockScreen.Verify(x => x.Hide(), Times.Once, "Hide should be called on the screen");
//         }
//
//         #endregion
//
//         #region Helper Methods
//
//         /// <summary>
//         /// Setup mock UI elements required for service initialization
//         /// </summary>
//         private void SetupMockUIElementsForInitialization()
//         {
//             // Setup all required UI elements that InitializeScreensAndPopups expects
//             var uiElementNames = new[]
//             {
//                 "UI_DebugScreen", "UI_SplashScreen", "UI_MainMenuScreen", "UI_GamePlayHUD",
//                 "UI_PauseScreen", "UI_OptionsScreen", "UI_LoadingScreen", "UI_NewGameScreen",
//                 "UI_CreditScreen", "UI_GameOverScreen", "UI_VictoryScreen"
//             };
//
//             foreach (var elementName in uiElementNames)
//             {
//                 var mockElement = new Mock<VisualElement>();
//                 _mockRootElement.Setup(x => x.Q<VisualElement>(elementName))
//                               .Returns(mockElement.Object);
//             }
//         }
//
//         #endregion
//     }
// }
