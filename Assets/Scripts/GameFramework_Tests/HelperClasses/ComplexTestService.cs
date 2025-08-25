namespace GameFramework.Tests.HelperClasses
{
    /// <summary>
    /// Test service with multiple constructor parameters for testing complex DI scenarios
    /// </summary>
    public class ComplexTestService
    {
        public ITestDependency Dependency1 { get; }
        public ITestService Dependency2 { get; }

        public ComplexTestService(ITestDependency dependency1, ITestService dependency2)
        {
            Dependency1 = dependency1 ?? throw new System.ArgumentNullException(nameof(dependency1));
            Dependency2 = dependency2 ?? throw new System.ArgumentNullException(nameof(dependency2));
        }
    }
}