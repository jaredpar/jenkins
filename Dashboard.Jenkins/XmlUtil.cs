using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Dashboard.Jenkins
{
    internal static class XmlUtil
    {
        internal static string ParseJobKind(XElement element)
        {
            return element.Name.LocalName;
        }
    }
}
