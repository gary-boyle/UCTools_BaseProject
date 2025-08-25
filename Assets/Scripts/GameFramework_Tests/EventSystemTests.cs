// using System;
// using System.Threading.Tasks;
// using GameFramework.Tests.HelperClasses;
// using NUnit.Framework;
// using UnityEngine.TestTools;
//
// namespace GameFramework.Tests
// {
//     /// <summary>
//     /// Comprehensive test suite for EventSystem class.
//     /// Tests event subscription, publishing, error handling, and lifecycle management.
//     /// 
//     /// Design: Isolated tests with event handler validation
//     /// Pros: Covers all event system functionality, tests error scenarios
//     /// Cons: Async testing complexity, event handler timing considerations
//     /// </summary>
//     public class EventSystemTests
//     {
//         private EventSystem.EventSystem _eventSystem;
//
//         [SetUp]
//         public void Setup()
//         {
//             _eventSystem = new EventSystem.EventSystem();
//         }
//
//         [TearDown]
//         public void TearDown()
//         {
//             _eventSystem.Shutdown();
//         }
//
//         #region Initialization Tests
//
//         [Test]
//         public async Task InitializeAsync_ShouldSetInitializedFlag()
//         {
//             // Arrange
//             Assert.IsFalse(_eventSystem.IsInitialized, "Should not be initialized initially");
//
//             // Act
//             await _eventSystem.InitializeAsync();
//
//             // Assert
//             Assert.IsTrue(_eventSystem.IsInitialized, "Should be initialized after InitializeAsync");
//         }
//
//         [Test]
//         public async Task InitializeAsync_CalledTwice_ShouldNotReinitialize()
//         {
//             // Arrange
//             await _eventSystem.InitializeAsync();
//             var wasInitialized = _eventSystem.IsInitialized;
//
//             // Act
//             await _eventSystem.InitializeAsync(); // Second call
//
//             // Assert
//             Assert.IsTrue(wasInitialized, "Should remain initialized");
//             Assert.IsTrue(_eventSystem.IsInitialized, "Should still be initialized");
//         }
//
//         [Test]
//         public void Shutdown_ShouldClearAndResetInitialization()
//         {
//             // Arrange
//             _eventSystem.InitializeAsync();
//             var testEvent = new TestEvent { Message = "Test" };
//             bool handlerCalled = false;
//             _eventSystem.Subscribe<TestEvent>(e => handlerCalled = true);
//
//             // Act
//             _eventSystem.Shutdown();
//
//             // Assert
//             Assert.IsFalse(_eventSystem.IsInitialized, "Should not be initialized after shutdown");
//             
//             // Verify handlers are cleared
//             _eventSystem.Publish(testEvent);
//             Assert.IsFalse(handlerCalled, "Handlers should be cleared after shutdown");
//         }
//
//         #endregion
//
//         #region Event Subscription Tests
//
//         [Test]
//         public void Subscribe_WithParameterHandler_ShouldReceiveEvents()
//         {
//             // Arrange
//             var testEvent = new TestEvent { Message = "Hello World" };
//             TestEvent receivedEvent = null;
//             
//             // Act
//             _eventSystem.Subscribe<TestEvent>(e => receivedEvent = e);
//             _eventSystem.Publish(testEvent);
//
//             // Assert
//             Assert.IsNotNull(receivedEvent, "Handler should receive event");
//             Assert.AreEqual("Hello World", receivedEvent.Message, "Event data should match");
//         }
//
//         [Test]
//         public void Subscribe_WithParameterlessHandler_ShouldReceiveEvents()
//         {
//             // Arrange
//             bool handlerCalled = false;
//             
//             // Act
//             _eventSystem.Subscribe<TestEvent>(() => handlerCalled = true);
//             _eventSystem.Publish<TestEvent>();
//
//             // Assert
//             Assert.IsTrue(handlerCalled, "Parameterless handler should be called");
//         }
//
//         [Test]
//         public void Subscribe_MultipleHandlers_ShouldCallAllHandlers()
//         {
//             // Arrange
//             int callCount = 0;
//             var testEvent = new TestEvent { Message = "Test" };
//
//             // Act
//             _eventSystem.Subscribe<TestEvent>(e => callCount++);
//             _eventSystem.Subscribe<TestEvent>(e => callCount++);
//             _eventSystem.Subscribe<TestEvent>(() => callCount++);
//             
//             _eventSystem.Publish(testEvent);
//
//             // Assert
//             Assert.AreEqual(2, callCount, "Only parameter handlers should be called for parameterized publish");
//         }
//
//         #endregion
//
//         #region Event Unsubscription Tests
//
//         [Test]
//         public void Unsubscribe_ParameterHandler_ShouldRemoveHandler()
//         {
//             // Arrange
//             var testEvent = new TestEvent { Message = "Test" };
//             bool handlerCalled = false;
//             Action<TestEvent> handler = e => handlerCalled = true;
//
//             _eventSystem.Subscribe(handler);
//
//             // Act
//             _eventSystem.Unsubscribe(handler);
//             _eventSystem.Publish(testEvent);
//
//             // Assert
//             Assert.IsFalse(handlerCalled, "Unsubscribed handler should not be called");
//         }
//
//         [Test]
//         public void Unsubscribe_ParameterlessHandler_ShouldRemoveHandler()
//         {
//             // Arrange
//             bool handlerCalled = false;
//             Action handler = () => handlerCalled = true;
//
//             _eventSystem.Subscribe<TestEvent>(handler);
//
//             // Act
//             _eventSystem.Unsubscribe<TestEvent>(handler);
//             _eventSystem.Publish<TestEvent>();
//
//             // Assert
//             Assert.IsFalse(handlerCalled, "Unsubscribed handler should not be called");
//         }
//
//         [Test]
//         public void Unsubscribe_NonExistentHandler_ShouldNotThrow()
//         {
//             // Arrange
//             Action<TestEvent> handler = e => { };
//
//             // Act & Assert
//             Assert.DoesNotThrow(() => _eventSystem.Unsubscribe(handler),
//                 "Unsubscribing non-existent handler should not throw");
//         }
//
//         #endregion
//
//         #region Event Publishing Tests
//
//         [Test]
//         public void Publish_WithNoSubscribers_ShouldNotThrow()
//         {
//             // Arrange
//             var testEvent = new TestEvent { Message = "Test" };
//
//             // Act & Assert
//             Assert.DoesNotThrow(() => _eventSystem.Publish(testEvent),
//                 "Publishing with no subscribers should not throw");
//         }
//
//         [Test]
//         public void Publish_HandlerThrowsException_ShouldContinueWithOtherHandlers()
//         {
//             // Arrange
//             bool secondHandlerCalled = false;
//             var testEvent = new TestEvent { Message = "Test" };
//
//             _eventSystem.Subscribe<TestEvent>(e => throw new Exception("Test exception"));
//             _eventSystem.Subscribe<TestEvent>(e => secondHandlerCalled = true);
//
//             // Act
//             LogAssert.Expect(UnityEngine.LogType.Error, 
//                 new System.Text.RegularExpressions.Regex(".*Error in event handler.*"));
//             _eventSystem.Publish(testEvent);
//
//             // Assert
//             Assert.IsTrue(secondHandlerCalled, 
//                 "Second handler should be called even if first throws exception");
//         }
//
//         #endregion
//
//         #region Clear Tests
//
//         [Test]
//         public void Clear_ShouldRemoveAllHandlers()
//         {
//             // Arrange
//             bool handlerCalled = false;
//             var testEvent = new TestEvent { Message = "Test" };
//             
//             _eventSystem.Subscribe<TestEvent>(e => handlerCalled = true);
//
//             // Act
//             _eventSystem.Clear();
//             _eventSystem.Publish(testEvent);
//
//             // Assert
//             Assert.IsFalse(handlerCalled, "Handlers should be cleared");
//         }
//
//         #endregion
//     }
// }
