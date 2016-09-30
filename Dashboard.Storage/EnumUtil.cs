using System;

namespace Dashboard
{
    public static class EnumUtil
    {
        public static T Parse<T>(string value, T defaultValue)
            where T : struct
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            T result;
            if (Enum.TryParse<T>(value, out result))
            {
                return result;
            }

            return defaultValue;
        }

        public static T Parse<T>(string value)
            where T : struct
        {
            return (T)Enum.Parse(typeof(T), value);
        }
    }
}
