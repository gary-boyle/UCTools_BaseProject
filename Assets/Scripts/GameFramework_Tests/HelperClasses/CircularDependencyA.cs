namespace GameFramework.Tests.HelperClasses
{
    public class CircularDependencyA
    {
        public CircularDependencyA(CircularDependencyB b) { }
    }
}