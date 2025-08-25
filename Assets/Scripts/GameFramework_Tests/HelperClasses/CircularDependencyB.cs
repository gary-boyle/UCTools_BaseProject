namespace GameFramework.Tests.HelperClasses
{
    public class CircularDependencyB
    {
        public CircularDependencyB(CircularDependencyA a) { }
    }
}