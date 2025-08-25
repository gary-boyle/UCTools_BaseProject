namespace GameFramework.Tests.HelperClasses
{
    /// <summary>
    /// Simple test service without dependencies for testing scenarios that don't need DI
    /// </summary>
    public class SimpleTestService : ITestService
    {
        public SimpleTestService()
        {
            IsInitialized = true;
        }

        public string GetValue() => "Simple Service: 0";
        public bool IsInitialized { get; }
    }
}