using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard.Azure.Builds
{
    internal static class LegacyUtil
    {
        /// <summary>
        /// Many older entity values didn't encode a host URI.  For those we pick up the default server.
        /// </summary>
        internal static Uri DefaultHost => new Uri("http://ci.dot.net");
    }
}
