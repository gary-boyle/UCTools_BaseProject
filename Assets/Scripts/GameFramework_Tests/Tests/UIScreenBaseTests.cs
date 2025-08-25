using NUnit.Framework;
using GameFramework.UI;
using UnityEngine.UIElements;
using System;

namespace GameFramework.Tests.UI
{
    [TestFixture]
    public class UIScreenBaseTests
    {
        private TestUIScreen _screen;
        private VisualElement _rootElement;

        [SetUp]
        public void SetUp()
        {
            _rootElement = new VisualElement();
            _screen = new TestUIScreen(_rootElement);
        }

        [Test]
        public void Constructor_WithValidRootElement_ShouldCreateScreen()
        {
            // Assert
            Assert.IsNotNull(_screen);
            Assert.IsFalse(_screen.IsVisible);
        }

        [Test]
        public void Constructor_WithNullRootElement_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestUIScreen(null));
        }

        [Test]
        public void Show_ShouldSetVisibilityToTrue()
        {
            // Act
            _screen.Show();

            // Assert
            Assert.IsTrue(_screen.IsVisible);
            Assert.AreEqual(DisplayStyle.Flex, _rootElement.style.display.value);
        }

        [Test]
        public void Hide_ShouldSetVisibilityToFalse()
        {
            // Arrange
            _screen.Show();

            // Act
            _screen.Hide();

            // Assert
            Assert.IsFalse(_screen.IsVisible);
            Assert.AreEqual(DisplayStyle.None, _rootElement.style.display.value);
        }

        [Test]
        public void Show_MultipleCallsShouldBeIdempotent()
        {
            // Act
            _screen.Show();
            _screen.Show();
            _screen.Show();

            // Assert
            Assert.IsTrue(_screen.IsVisible);
        }

        [Test]
        public void Hide_MultipleCallsShouldBeIdempotent()
        {
            // Arrange
            _screen.Show();

            // Act
            _screen.Hide();
            _screen.Hide();
            _screen.Hide();

            // Assert
            Assert.IsFalse(_screen.IsVisible);
        }

        private class TestUIScreen : UIScreen
        {
            public TestUIScreen(VisualElement rootElement) : base(rootElement) { }
        }
    }
}
