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
        internal static JobKind ParseJobKind(XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "buildFlow": return JobKind.Flow;
                case "folder": return JobKind.Folder;
                default: return JobKind.Build;
            }
        }
    }
}
