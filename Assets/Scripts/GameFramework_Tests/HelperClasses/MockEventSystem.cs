// using System.Threading.Tasks;
// using GameFramework.EventSystem.Interfaces;
//
// namespace GameFramework.Tests.HelperClasses
// {
//     public class MockEventSystem : IEventSystem
//     {
//         public bool IsInitialized { get; private set; }
//         
//         public Task InitializeAsync()
//         {
//             IsInitialized = true;
//             return Task.CompletedTask;
//         }
//
//         public void Shutdown() => IsInitialized = false;
//         public void Subscribe<T>(System.Action<T> handler) where T : class { }
//         public void Subscribe<T>(System.Action handler) { }
//         public void Unsubscribe<T>(System.Action<T> handler) where T : class { }
//         public void Unsubscribe<T>(System.Action handler) { }
//         public void Publish<T>(T eventData) where T : class { }
//         public void Publish<T>() { }
//         public void Clear() { }
//     }
// }