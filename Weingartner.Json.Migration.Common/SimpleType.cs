using System;
using System.Reflection;

namespace Weingartner.Json.Migration.Common
{
    public class SimpleType
    {
        public SimpleType(string fullName, AssemblyName assemblyName)
        {
            FullName = fullName;
            AssemblyName = assemblyName;
        }

        public string FullName { get; }
        public AssemblyName AssemblyName { get; }
        public string AssemblyQualifiedName => $"{FullName}, {AssemblyName}";
        public object Name => FullName.Substring(FullName.LastIndexOf(".", StringComparison.Ordinal) + 1);
    }
}