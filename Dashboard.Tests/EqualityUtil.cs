using System;

namespace Dashboard.Tests
{
    public static class EqualityUtil
    {
        public static void RunAll<T>(
            this EqualityUnit<T> unit,
            Func<T, T, bool> compEqualsOperator,
            Func<T, T, bool> compNotEqualsOperator)
        {
            var util = new EqualityUtil<T>(unit, compEqualsOperator, compNotEqualsOperator);
            util.RunAll();
        }

        public static void RunAll<T>(
            this EqualityUnit<T> unit,
            bool checkIEquatable)
        {
            var util = new EqualityUtil<T>(unit, null, null);
            util.RunAll(checkIEquatable);
        }
    }
}
