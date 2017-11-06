using Google.Protobuf.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plaisted.ProtobufJsonGen
{
    public class ProtoFile
    {
        public string BaseNamespace { get; set; }
        public string PackageFileName { get; set; }
        public List<string> Namespaces { get; set; }
        public List<string> SubNamespaces { get; set; } = new List<string>();
        public List<(string Contents, string Namespace, string FullName)> Enums { get; }
            = new List<(string Contents, string Namespace,string FullName)>();
        public List<(string Contents, string Namespace, string FullName)> Classes { get; } = 
            new List<(string Contents, string Namespace, string FullName)>();
        public List<string> ReferencedTypes { get; set; } = new List<string>();
        private List<string> importList = new List<string> { "import { __protogen } from './proto-mappers';" };
        public ProtoFile(string packageName)
        {
            BaseNamespace = packageName;
            PackageFileName = packageName + ".ts";
            Namespaces = packageName.Split(".").ToList();
        }
        
        internal void AddReference(string import)
        {
            importList.Add(import);
        }

        public void AddMessage(Message message)
        {
            Classes.Add((message.GetContents(), GetNamespaceFromFullName(message.Descriptor.FullName), message.Descriptor.FullName));
            ReferencedTypes.AddRange(message.ReferencedTypes);
        }

        public string GetContents()
        {
            var result = GetImports() + Environment.NewLine +
                $"export namespace {BaseNamespace} {{" + Environment.NewLine
                + GetNamespaceContents(BaseNamespace).AddTabs(1)
                + Environment.NewLine + "}";

            return string.Join(Environment.NewLine,
                result.Split(Environment.NewLine).Select(l => { if (string.IsNullOrWhiteSpace(l)) { return ""; } else { return l; } }));
        }

        private string GetImports()
        {
            return string.Join(Environment.NewLine, importList);
        }
        private string GetNamespaceContents(string nameSpace)
        {
            var contents = string.Join(Environment.NewLine, Classes.Where(c => c.Namespace == nameSpace).Select(c=>c.Contents));
            contents = contents + Environment.NewLine + string.Join(Environment.NewLine, Enums.Where(c => c.Namespace == nameSpace).Select(e => e.Contents));
            var toAdd = SubNamespaces.Where(ns => ns.StartsWith(nameSpace+"."))
                .Select(ns=> (Namespace: ns, Sub: ns.Remove(0, nameSpace.Length+1).Split('.')[0])).ToList();
            foreach (var sub in toAdd.Select(n=>n.Sub).Distinct())
            {
                var subContents = "";
                //foreach (var ns in toAdd.Where(n=>n.Sub == sub))
                //{
                    subContents += GetNamespaceContents(nameSpace + "." + sub) + Environment.NewLine;
                //}

                contents = contents + Environment.NewLine +
                    $"export namespace {sub} {{" + Environment.NewLine + subContents.AddTabs(1) + Environment.NewLine + "}";
            }
            return contents;
        }

        private string GetNamespaceFromFullName(string fullName)
        {
            var split = fullName.Split('.');
            return string.Join('.', split.Take(split.Count() - 1));
        }

        public void AddEnum(EnumDescriptor enumToAdd)
        {
            Enums.Add((GetEnumClass(enumToAdd), GetNamespaceFromFullName(enumToAdd.FullName), enumToAdd.FullName));
            ReferencedTypes.Add(enumToAdd.FullName);
            //enumToAdd.f
        }
        public string GetEnumClass(EnumDescriptor enumField)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("export enum " + enumField.Name + " {" + Environment.NewLine);
            foreach (var value in enumField.Values)
            {
                builder.Append($"    {value.Name} = {value.Number}," + Environment.NewLine);
            }
            builder.Length -= Environment.NewLine.Length + 1;
            builder.Append(Environment.NewLine + "}" + Environment.NewLine);
            return builder.ToString();
        }
    }

    public static class StringExtensions
    {
        public static string AddTabs(this string section, int tabs)
        {
            return String.Join(Environment.NewLine, section.Split(Environment.NewLine).Select(l => Tabs(tabs) + l));
        }
        private static string Tabs(int tabs)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < tabs; i++)
            {
                builder.Append("    ");
            }
            return builder.ToString();
        }
    }
}
