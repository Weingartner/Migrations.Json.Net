using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Weingartner.DataMigration.Fody
{
    public static class TypeReferenceExtensions
    {
        public static bool IsProbablyEqualTo(this TypeDefinition t1, TypeDefinition t2)
        {
            return t1.FullName == t2.FullName
                && t1.Module.FullyQualifiedName == t2.Module.FullyQualifiedName;
        }

        public static bool IsProbablyEqualTo(this TypeReference t1, TypeReference t2)
        {
            return t1.FullName == t2.FullName;
        }

        public static bool HasInterface(this TypeReference type, TypeDefinition interfaceType)
        {
            return GetInterfaces(type).Any(i => i.Resolve().IsProbablyEqualTo(interfaceType));
        }

        public static IEnumerable<TypeReference> GetInterfaces(this TypeReference type)
        {
            var typeDef = type.Resolve();
            foreach (var @interface in typeDef.Interfaces)
            {
                yield return @interface;
                foreach (var baseInterface in GetInterfaces(@interface))
                {
                    yield return baseInterface;
                }
            }

            if (typeDef.BaseType != null)
            {
                foreach (var @interface in GetInterfaces(typeDef.BaseType))
                {
                    yield return @interface;
                }
            }
        } 
    }
}
