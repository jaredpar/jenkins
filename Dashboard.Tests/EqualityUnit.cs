namespace Roslyn.Jenkins.Tests
{
    public static class EqualityUnit
    {
        public static EqualityUnit<T> Create<T>(T value)
        {
            return new EqualityUnit<T>(value);
        }
    }
}
