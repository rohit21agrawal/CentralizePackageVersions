using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CentralizePackageVersions
{
    class ProjectFileUtils
    {
        public static void WriteXmlToFile(XDocument xml, FileStream stream)
        {
            var unicodeEncoding = new UTF8Encoding();
            var xmlString = xml.ToString();
            stream.SetLength(0);
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(unicodeEncoding.GetBytes(xmlString), 0, unicodeEncoding.GetByteCount(xmlString));
        }

        private static void AddCondition(string condition, XElement subItem)
        {
            if (TrimAndGetNullForEmpty(condition) != null)
            {
                subItem.Add(new XAttribute(XName.Get("Condition"), condition));
            }
        }

        public static void AddProperty(XDocument doc, string propertyName, string propertyValue)
        {
            AddProperty(doc, propertyName, propertyValue, condition: null);
        }

        public static void AddProperty(XDocument doc, string propertyName, string propertyValue, string condition)
        {
            var lastPropGroup = doc.Root.Elements().Last(e => e.Name.LocalName == "PropertyGroup");
            var element = new XElement(XName.Get(propertyName), propertyValue);

            AddCondition(condition, element);

            lastPropGroup.Add(element);
        }

        public static string TrimAndGetNullForEmpty(string s)
        {
            if (s == null)
            {
                return null;
            }

            s = s.Trim();

            return s.Length == 0 ? null : s;
        }
    }
}
