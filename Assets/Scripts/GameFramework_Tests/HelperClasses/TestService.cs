// namespace GameFramework.Tests.HelperClasses
// {
//     public class TestService : ITestService
//     {
//         private readonly ITestDependency _dependency;
//         
//         public TestService(ITestDependency dependency)
//         {
//             _dependency = dependency;
//             IsInitialized = true;
//         }
//
//         public TestService()
//         {
//             IsInitialized = true;
//         }
//
//         public string GetValue() => $"Service: {_dependency?.GetNumber() ?? 0}";
//         public bool IsInitialized { get; }
//     }
// }