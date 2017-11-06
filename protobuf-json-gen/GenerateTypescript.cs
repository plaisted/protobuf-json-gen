using Google.Protobuf;
using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Plaisted.ProtobufJsonGen
{
    public class GenerateTypescript
    {
        public static void FromDll(string path)
        {
            var types = Assembly.LoadFile(path).GetTypesWithInterface(typeof(IMessage));
            var messages = types.Select(t => new Message(t)).ToList();
            var packageNames = messages.Select(g => g.Descriptor.File.Package).Distinct().ToList(); //.Select(p=>new PackageInfo(p)).ToList();
            IDictionary<string, EnumDescriptor> enums = new Dictionary<string, EnumDescriptor>();
            messages.ForEach(g => g.Descriptor.Fields.InDeclarationOrder().Where(f => f.FieldType == FieldType.Enum).Select(e=>e.EnumType).ToList().ForEach(e => enums[e.FullName] = e));
            foreach (var e in enums)
            {
                var split = e.Key.Split('.');
                split = split.Take(split.Count() - 1).ToArray();
                if (split.Count() > 0)
                {
                    packageNames.Add(string.Join('.', split));
                }
                packageNames = packageNames.Distinct().ToList();
            }

            var packages = new List<ProtoFile>(); // packageNames.Select(p => new PackageInfo(p)).ToList();
            var removed = new List<string>();
            foreach (var package in packageNames.OrderBy(p=>p.Count(c=>c == '.')).ToList())
            {
                if (removed.Contains(package)) { continue; }
                var subNames = package;
                var useSubname = false;
                while (subNames.Length > 0)
                {
                    if (packageNames.Where(p => p.StartsWith(subNames) && p != package).Count() > 0)
                    {
                        useSubname = true;
                        break;
                    }
                    var split = subNames.Split('.');
                    subNames = string.Join('.', split.Take(split.Count() - 1));
                }
                
                if (!useSubname)
                {
                    subNames = package;
                }

                var pi = new ProtoFile(subNames);
                packages.Add(pi);
                packageNames.Where(p => p.StartsWith(subNames + ".")).ToList();
                packageNames.ForEach(p => {
                    removed.Add(p);
                    pi.SubNamespaces.Add(p);
                });
            }

            //add messages and enums for each file
            foreach (var package in packages)
            {
                enums.Where(e => e.Value.FullName.StartsWith(package.BaseNamespace)).ToList().ForEach(p => package.AddEnum(p.Value));
                messages.Where(g => g.Descriptor.FullName.StartsWith(package.BaseNamespace)).ToList().ForEach(g => package.AddMessage(g));
            }

            //add external refs and create content
            foreach (var package in packages)
            {
                var externals = package.ReferencedTypes.Where(t => !t.StartsWith(package.BaseNamespace+".")).ToList();
                foreach (var type in externals)
                {
                    var result = packages.Where(p => type.StartsWith(p.BaseNamespace)).FirstOrDefault();
                    if (result == null)
                    {
                        throw new ApplicationException("Referenced type not found in referenced files");
                    }
                    var baseNamespace = type.Split('.')[0];
                    package.AddReference($"import {{ {baseNamespace} }} from './{result.BaseNamespace}.ts';");
                }
                var contents = package.GetContents();
           }
        }
    }
}
