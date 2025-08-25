// // Tests/DIContainerTests.cs
// using System;
// using NUnit.Framework;
// using GameFramework.Core;
// using GameFramework.Tests.HelperClasses;
//
// namespace GameFramework.Tests
// {
//     /// <summary>
//     /// Comprehensive test suite for DIContainer class.
//     /// Tests singleton registration, factory registration, dependency resolution,
//     /// circular dependency detection, and error handling.
//     /// 
//     /// Design: Isolated unit tests with proper setup/teardown
//     /// Pros: Thorough coverage, edge case testing, clear test structure
//     /// Cons: Requires mock objects, potential test maintenance overhead
//     /// </summary>
//     public class DIContainerTests
//     {
//         private DIContainer _container;
//
//         [SetUp]
//         public void Setup()
//         {
//             // Create a fresh container instance for each test
//             _container = new DIContainer();
//             _container.Clear();
//         }
//
//         [TearDown]
//         public void TearDown()
//         {
//             // Clean up after each test
//             _container.Clear();
//         }
//
//         #region Singleton Registration Tests
//
//         [Test]
//         public void RegisterSingleton_WithInstance_ShouldReturnSameInstance()
//         {
//             // Arrange
//             var testService = new TestService();
//
//             // Act
//             _container.RegisterSingleton<ITestService>(testService);
//             var resolved1 = _container.Resolve<ITestService>();
//             var resolved2 = _container.Resolve<ITestService>();
//
//             // Assert
//             Assert.AreSame(testService, resolved1, "Should return the registered instance");
//             Assert.AreSame(resolved1, resolved2, "Should return the same instance on multiple resolves");
//         }
//
//         [Test]
//         public void RegisterSingleton_WithType_ShouldCreateSingleInstance()
//         {
//             // Arrange & Act
//             _container.RegisterSingleton<ITestService, TestService>();
//             var resolved1 = _container.Resolve<ITestService>();
//             var resolved2 = _container.Resolve<ITestService>();
//
//             // Assert
//             Assert.IsInstanceOf<TestService>(resolved1, "Should create instance of registered type");
//             Assert.AreSame(resolved1, resolved2, "Should return the same singleton instance");
//         }
//
//         [Test]
//         public void RegisterSingleton_ConcreteType_ShouldCreateSingleInstance()
//         {
//             // Arrange & Act
//             _container.RegisterSingleton<TestService>();
//             var resolved1 = _container.Resolve<TestService>();
//             var resolved2 = _container.Resolve<TestService>();
//
//             // Assert
//             Assert.IsInstanceOf<TestService>(resolved1);
//             Assert.AreSame(resolved1, resolved2);
//         }
//
//         #endregion
//
//         #region Factory Registration Tests
//
//         [Test]
//         public void RegisterFactory_ShouldCreateNewInstanceEachTime()
//         {
//             // Arrange
//             _container.RegisterFactory<ITestService>(() => new TestService());
//
//             // Act
//             var resolved1 = _container.Resolve<ITestService>();
//             var resolved2 = _container.Resolve<ITestService>();
//
//             // Assert
//             Assert.IsInstanceOf<TestService>(resolved1);
//             Assert.IsInstanceOf<TestService>(resolved2);
//             Assert.AreNotSame(resolved1, resolved2, "Factory should create new instances each time");
//         }
//
//         [Test]
//         public void RegisterTransient_ShouldCreateNewInstanceEachTime()
//         {
//             // Arrange & Act
//             _container.RegisterTransient<ITestService, TestService>();
//             var resolved1 = _container.Resolve<ITestService>();
//             var resolved2 = _container.Resolve<ITestService>();
//
//             // Assert
//             Assert.IsInstanceOf<TestService>(resolved1);
//             Assert.IsInstanceOf<TestService>(resolved2);
//             Assert.AreNotSame(resolved1, resolved2);
//         }
//
//         #endregion
//
//         #region Constructor Injection Tests
//
//         [Test]
//         public void Resolve_WithDependencies_ShouldInjectConstructorParameters()
//         {
//             // Arrange
//             _container.RegisterSingleton<ITestDependency, TestDependency>();
//             _container.RegisterSingleton<ITestService, TestService>();
//
//             // Act
//             var resolved = _container.Resolve<ITestService>();
//
//             // Assert
//             Assert.IsInstanceOf<TestService>(resolved);
//             Assert.AreEqual("Service: 42", resolved.GetValue(), "Dependencies should be injected");
//         }
//
//         [Test]
//         public void Resolve_ConcreteTypeWithoutRegistration_ShouldCreateInstance()
//         {
//             // Arrange
//             _container.RegisterSingleton<ITestDependency, TestDependency>();
//
//             // Act
//             var resolved = _container.Resolve<TestService>();
//
//             // Assert
//             Assert.IsInstanceOf<TestService>(resolved);
//             Assert.IsTrue(resolved.IsInitialized);
//         }
//
//         #endregion
//
//         #region Error Handling Tests
//
//         [Test]
//         public void Resolve_UnregisteredInterface_ShouldThrowException()
//         {
//             // Act & Assert
//             Assert.Throws<InvalidOperationException>(() => _container.Resolve<ITestService>(),
//                 "Should throw exception for unregistered interface");
//         }
//
//         [Test]
//         public void Resolve_CircularDependency_ShouldThrowException()
//         {
//             // Arrange
//             _container.RegisterSingleton<CircularDependencyA>();
//             _container.RegisterSingleton<CircularDependencyB>();
//
//             // Act & Assert
//             var exception = Assert.Throws<InvalidOperationException>(() => 
//                 _container.Resolve<CircularDependencyA>());
//             
//             Assert.IsTrue(exception.Message.Contains("Circular dependency"),
//                 "Exception should indicate circular dependency");
//         }
//
//         #endregion
//
//         #region Registration Check Tests
//
//         [Test]
//         public void IsRegistered_WithRegisteredType_ShouldReturnTrue()
//         {
//             // Arrange
//             _container.RegisterSingleton<ITestService, TestService>();
//
//             // Act & Assert
//             Assert.IsTrue(_container.IsRegistered<ITestService>());
//             Assert.IsTrue(_container.IsRegistered(typeof(ITestService)));
//         }
//
//         [Test]
//         public void IsRegistered_WithUnregisteredType_ShouldReturnFalse()
//         {
//             // Act & Assert
//             Assert.IsFalse(_container.IsRegistered<ITestService>());
//             Assert.IsFalse(_container.IsRegistered(typeof(ITestService)));
//         }
//
//         #endregion
//
//         #region Clear Tests
//
//         [Test]
//         public void Clear_ShouldRemoveAllRegistrations()
//         {
//             // Arrange
//             _container.RegisterSingleton<ITestService, TestService>();
//             _container.RegisterFactory<ITestDependency>(() => new TestDependency());
//
//             // Act
//             _container.Clear();
//
//             // Assert
//             Assert.IsFalse(_container.IsRegistered<ITestService>());
//             Assert.IsFalse(_container.IsRegistered<ITestDependency>());
//         }
//
//         #endregion
//     }
// }
