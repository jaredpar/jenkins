using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Dashboard.Helpers
{
    public static class Extensions
    {
        public static int GetParamInt(this HttpRequestBase request, string name, int defaultValue = 0)
        {
            var str = request[name];
            int value;
            if (string.IsNullOrEmpty(str) || !int.TryParse(str, out value))
            {
                return defaultValue;
            }

            return value;
        }
    }
}