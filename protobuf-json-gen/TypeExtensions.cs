using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Plaisted.ProtobufJsonGen
{
    public static class TypeExtensions
    {
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
        public static IEnumerable<Type> GetTypesWithInterface(this Assembly assembly, Type interfaceType)
        {
            return assembly.GetLoadableTypes().Where(interfaceType.IsAssignableFrom).ToList();
        }
    }
}
