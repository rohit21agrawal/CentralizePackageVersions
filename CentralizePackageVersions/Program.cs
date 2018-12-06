using System;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;

namespace CentralizePackageVersions
{
    class Program
    {
        static void Main(string[] args)
        {
            var files = Directory.GetFiles(@"E:\iotexp\Azure-IoT-ConnectedCar-Core\", "*.csproj", SearchOption.AllDirectories);
            var dependencyFilePath = @"E:\iotexp\Azure-IoT-ConnectedCar-Core\infra\build\DependencyVersion.props";


            var packageRefDictionary = new Dictionary<string, PackageReferenceModel>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
                {
                    try
                    {
                        var xml = XDocument.Load(stream);
                        var ns = xml.Root.GetDefaultNamespace();
                        var itemGroups = xml.Root.Elements().Where(t => t.Name.LocalName == "ItemGroup" && t.HasElements);

                        foreach (var item in itemGroups)
                        {
                            var packageRefs = item.Elements(XName.Get("PackageReference", ns.NamespaceName));
                            foreach (var reference in packageRefs)
                            {
                                if (reference.HasElements || reference.HasAttributes)
                                {
                                    ConvertElementsToAttributes(reference, ns.NamespaceName);
                                    var name = reference.Attribute(XName.Get("Include")).Value;
                                    var version = reference.Attribute(XName.Get("Version")).Value;

                                    var referenceModel = GetPackageReferenceModel(name, version, packageRefDictionary);
                                    reference.SetAttributeValue(XName.Get("Version"), $@"$({referenceModel.XmlEntry})");
                                }
                            }
                        }
                        ProjectFileUtils.WriteXmlToFile(xml, stream);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception thrown while trying to read file {file}. {e.Message}");
                    }
                }
            }

            UpdateDependencyVersionFile(dependencyFilePath, packageRefDictionary);

            Console.ReadLine();
        }

        private static void UpdateDependencyVersionFile(string filePath, Dictionary<string, PackageReferenceModel> packageRefDict)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            {
                var xml = XDocument.Load(stream);
                var ns = xml.Root.GetDefaultNamespace();
                foreach (var dep in packageRefDict.Keys)
                {
                    ProjectFileUtils.AddProperty(xml, packageRefDict[dep].XmlEntry, packageRefDict[dep].Version);
                }
                ProjectFileUtils.WriteXmlToFile(xml, stream);
            }
        }

        private static void ConvertElementsToAttributes(XElement element, string namespaceName)
        {
            var attributes = element.Elements();
            var dictionary = new Dictionary<string, string>();
            foreach (var attr in attributes)
            {
                var name = attr.Name.LocalName;
                var value = attr.Value;
                dictionary[name] = value;
            }
            element.RemoveNodes();

            foreach (var newAttr in dictionary.Keys)
            {
                element.SetAttributeValue(XName.Get(newAttr), dictionary[newAttr]);
            }
        }

        private static PackageReferenceModel GetPackageReferenceModel(string name, string version, Dictionary<string, PackageReferenceModel> packageRefDictionary)
        {
            if (packageRefDictionary.ContainsKey(name))
            {
                return packageRefDictionary[name];
            }
            else
            {
                var newName = name.Replace(".", "").Trim() + "Version";
                packageRefDictionary[name] = new PackageReferenceModel()
                {
                    Name = name,
                    XmlEntry = newName,
                    Version = version
                };

                return packageRefDictionary[name];
            }
        }
    }

    public struct PackageReferenceModel
    {
        public string Name;
        public string XmlEntry;
        public string Version;
    }
}
