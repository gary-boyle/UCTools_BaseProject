using System;
using NUnit.Framework;
using GameFramework.Core;
using GameFramework.Tests.HelperClasses;

namespace GameFramework.Tests
{
    /// <summary>
    /// Comprehensive test suite for DIContainer class.
    /// Tests singleton registration, factory registration, dependency resolution,
    /// circular dependency detection, and error handling.
    /// 
    /// Design: Isolated unit tests with proper setup/teardown and dependency management
    /// Pros: Thorough coverage, edge case testing, clear test structure, proper DI testing
    /// Cons: Requires mock objects, potential test maintenance overhead
    /// </summary>
    public class DIContainerTests
    {
        private DIContainer _container;

        [SetUp]
        public void Setup()
        {
            // Create a fresh container instance for each test
            _container = new DIContainer();
            _container.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            _container.Clear();
        }

        #region Singleton Registration Tests

        [Test]
        public void RegisterSingleton_WithInstance_ShouldReturnSameInstance()
        {
            // Arrange
            var testDependency = new TestDependency();
            var testService = new TestService(testDependency);

            // Act
            _container.RegisterSingleton<ITestService>(testService);
            var resolved1 = _container.Resolve<ITestService>();
            var resolved2 = _container.Resolve<ITestService>();

            // Assert
            Assert.AreSame(testService, resolved1, "Should return the registered instance");
            Assert.AreSame(resolved1, resolved2, "Should return the same instance on multiple resolves");
        }

        [Test]
        public void RegisterSingleton_WithType_ShouldCreateSingleInstance()
        {
            // Arrange - Register dependency first
            _container.RegisterSingleton<ITestDependency, TestDependency>();
            
            // Act
            _container.RegisterSingleton<ITestService, TestService>();
            var resolved1 = _container.Resolve<ITestService>();
            var resolved2 = _container.Resolve<ITestService>();

            // Assert
            Assert.IsInstanceOf<TestService>(resolved1, "Should create instance of registered type");
            Assert.AreSame(resolved1, resolved2, "Should return the same singleton instance");
        }

        [Test]
        public void RegisterSingleton_ConcreteType_ShouldCreateSingleInstance()
        {
            // Arrange - Register dependency first
            _container.RegisterSingleton<ITestDependency, TestDependency>();
            
            // Act
            _container.RegisterSingleton<TestService>();
            var resolved1 = _container.Resolve<TestService>();
            var resolved2 = _container.Resolve<TestService>();

            // Assert
            Assert.IsInstanceOf<TestService>(resolved1);
            Assert.AreSame(resolved1, resolved2);
        }

        #endregion

        #region Factory Registration Tests

        [Test]
        public void RegisterFactory_ShouldCreateNewInstanceEachTime()
        {
            // Arrange
            var testDependency = new TestDependency();
            _container.RegisterFactory<ITestService>(() => new TestService(testDependency));

            // Act
            var resolved1 = _container.Resolve<ITestService>();
            var resolved2 = _container.Resolve<ITestService>();

            // Assert
            Assert.IsInstanceOf<TestService>(resolved1);
            Assert.IsInstanceOf<TestService>(resolved2);
            Assert.AreNotSame(resolved1, resolved2, "Factory should create new instances each time");
        }

        [Test]
        public void RegisterTransient_ShouldCreateNewInstanceEachTime()
        {
            // Arrange - Register dependency first
            _container.RegisterSingleton<ITestDependency, TestDependency>();
            
            // Act
            _container.RegisterTransient<ITestService, TestService>();
            var resolved1 = _container.Resolve<ITestService>();
            var resolved2 = _container.Resolve<ITestService>();

            // Assert
            Assert.IsInstanceOf<TestService>(resolved1);
            Assert.IsInstanceOf<TestService>(resolved2);
            Assert.AreNotSame(resolved1, resolved2);
        }

        #endregion

        #region Constructor Injection Tests

        [Test]
        public void Resolve_WithDependencies_ShouldInjectConstructorParameters()
        {
            // Arrange
            _container.RegisterSingleton<ITestDependency, TestDependency>();
            _container.RegisterSingleton<ITestService, TestService>();

            // Act
            var resolved = _container.Resolve<ITestService>();

            // Assert
            Assert.IsInstanceOf<TestService>(resolved);
            Assert.AreEqual("Service: 42", resolved.GetValue(), "Dependencies should be injected");
        }

        [Test]
        public void Resolve_ConcreteTypeWithoutRegistration_ShouldCreateInstance()
        {
            // Arrange - Register the dependency that TestService needs
            _container.RegisterSingleton<ITestDependency, TestDependency>();

            // Act
            var resolved = _container.Resolve<TestService>();

            // Assert
            Assert.IsInstanceOf<TestService>(resolved);
            Assert.IsTrue(resolved.IsInitialized);
        }

        [Test]
        public void Resolve_SimpleServiceWithoutDependencies_ShouldCreateInstance()
        {
            // Act - SimpleTestService has no dependencies
            var resolved = _container.Resolve<SimpleTestService>();

            // Assert
            Assert.IsInstanceOf<SimpleTestService>(resolved);
            Assert.IsTrue(resolved.IsInitialized);
        }

        [Test]
        public void Resolve_ComplexDependencyChain_ShouldResolveAll()
        {
            // Arrange
            _container.RegisterSingleton<ITestDependency, TestDependency>();
            _container.RegisterSingleton<ITestService, SimpleTestService>(); // Use SimpleTestService to avoid circular dependency
            _container.RegisterSingleton<ComplexTestService>();

            // Act
            var resolved = _container.Resolve<ComplexTestService>();

            // Assert
            Assert.IsNotNull(resolved);
            Assert.IsNotNull(resolved.Dependency1);
            Assert.IsNotNull(resolved.Dependency2);
            Assert.IsInstanceOf<TestDependency>(resolved.Dependency1);
            Assert.IsInstanceOf<SimpleTestService>(resolved.Dependency2);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Resolve_UnregisteredInterface_ShouldThrowException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _container.Resolve<ITestService>(),
                "Should throw exception for unregistered interface");
        }

        [Test]
        public void Resolve_CircularDependency_ShouldThrowException()
        {
            // Arrange
            _container.RegisterSingleton<CircularDependencyA>();
            _container.RegisterSingleton<CircularDependencyB>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _container.Resolve<CircularDependencyA>());
            
            Assert.IsTrue(exception.Message.Contains("Circular dependency"),
                "Exception should indicate circular dependency");
        }

        [Test]
        public void Resolve_MissingDependency_ShouldThrowException()
        {
            // Arrange - Register TestService but not its dependency
            _container.RegisterSingleton<ITestService, TestService>();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _container.Resolve<ITestService>());
            
            Assert.IsTrue(exception.Message.Contains("No registration found for type ITestDependency"),
                "Should throw exception for missing dependency");
        }

        #endregion

        #region Registration Check Tests

        [Test]
        public void IsRegistered_WithRegisteredType_ShouldReturnTrue()
        {
            // Arrange
            _container.RegisterSingleton<ITestDependency, TestDependency>();
            _container.RegisterSingleton<ITestService, TestService>();

            // Act & Assert
            Assert.IsTrue(_container.IsRegistered<ITestService>());
            Assert.IsTrue(_container.IsRegistered(typeof(ITestService)));
        }

        [Test]
        public void IsRegistered_WithUnregisteredType_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(_container.IsRegistered<ITestService>());
            Assert.IsFalse(_container.IsRegistered(typeof(ITestService)));
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ShouldRemoveAllRegistrations()
        {
            // Arrange
            _container.RegisterSingleton<ITestDependency, TestDependency>();
            _container.RegisterSingleton<ITestService, TestService>();
            _container.RegisterFactory<SimpleTestService>(() => new SimpleTestService());

            // Act
            _container.Clear();

            // Assert
            Assert.IsFalse(_container.IsRegistered<ITestService>());
            Assert.IsFalse(_container.IsRegistered<ITestDependency>());
            Assert.IsFalse(_container.IsRegistered<SimpleTestService>());
        }

        #endregion
    }
}
