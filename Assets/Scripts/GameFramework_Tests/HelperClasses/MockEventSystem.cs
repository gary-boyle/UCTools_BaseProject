using System.Threading.Tasks;
using GameFramework.EventSystem.Interfaces;

namespace GameFramework.Tests.HelperClasses
{
    /// <summary>
    /// Mock implementation of IEventSystem for testing
    /// </summary>
    public class MockEventSystem : IEventSystem
    {
        public bool IsInitialized { get; private set; }
        
        public Task InitializeAsync()
        {
            IsInitialized = true;
            return Task.CompletedTask;
        }

        public void Shutdown() => IsInitialized = false;
        
        public void Subscribe<T>(System.Action<T> handler) where T : class 
        {
            // Mock implementation - could store handlers if needed for testing
        }
        
        public void Subscribe<T>(System.Action handler) 
        {
            // Mock implementation
        }
        
        public void Unsubscribe<T>(System.Action<T> handler) where T : class 
        {
            // Mock implementation
        }
        
        public void Unsubscribe<T>(System.Action handler) 
        {
            // Mock implementation
        }
        
        public void Publish<T>(T eventData) where T : class 
        {
            // Mock implementation - could trigger test events if needed
        }
        
        public void Publish<T>() 
        {
            // Mock implementation
        }
        
        public void Clear() 
        {
            // Mock implementation
        }
    }
}