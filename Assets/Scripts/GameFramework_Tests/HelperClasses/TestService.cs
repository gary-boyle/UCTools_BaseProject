namespace GameFramework.Tests.HelperClasses
{
    /// <summary>
    /// Test service with optional dependency injection
    /// Modified to have clearer constructor behavior for testing
    /// </summary>
    public class TestService : ITestService
    {
        private readonly ITestDependency _dependency;
        
        public TestService(ITestDependency dependency)
        {
            _dependency = dependency ?? throw new System.ArgumentNullException(nameof(dependency));
            IsInitialized = true;
        }

        public string GetValue() => $"Service: {_dependency?.GetNumber() ?? 0}";
        public bool IsInitialized { get; }
    }
} 