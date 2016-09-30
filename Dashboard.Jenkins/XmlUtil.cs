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
