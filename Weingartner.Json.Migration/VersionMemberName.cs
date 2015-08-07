using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration
{
    public static class VersionMemberName
    {
        public static string VersionPropertyName => "Version";

        public static IEnumerable<string> SupportedVersionPropertyNames
        {
            get
            {
                yield return VersionPropertyName;
                yield return "<>Version";
            }
        }


        public static int GetCurrentVersion(Type type)
        {
            return GetAndVerifyMigrationMethods(type)
                .Select(v => v.ToVersion)
                .Concat(new[] { 0 })
                .Max();
        }

        private static IReadOnlyList<MigrationMethod> ParseMigrationMethods(IEnumerable<MethodInfo> methods)
        {
            var migrationMethods = methods
                .Select(GetMigrationMethod)
                .Where(x => x != null)
                .OrderBy(m => m.ToVersion)
                .ToList();

            if (migrationMethods.Count == 0)
                return migrationMethods;

            var firstVersion = migrationMethods[0];
            if (firstVersion.ToVersion != 1)
                throw new MigrationException($"Migrations must start with '{MigrationMethod.NamePrefix}1. but starts with '{firstVersion.Name}");

            var isNonConsecutive = migrationMethods
                .Select((i, j) => i.ToVersion - j)
                .Distinct()
                .Skip(1)
                .Any();
            if (isNonConsecutive)
                throw new MigrationException($"Migrations must be consecutive but got versions {string.Join(", ", migrationMethods.Select(m => m.ToVersion))}");

            return migrationMethods;
        }

        public static MigrationMethod GetMigrationMethod(MethodInfo method)
        {
            var declaringType = new SimpleType(GetTypeName(method.DeclaringType), GetAssemblyName(method.DeclaringType));
            var parameters = method.GetParameters()
                .Select(p => new MethodParameter(new SimpleType(GetTypeName(p.ParameterType), GetAssemblyName(p.ParameterType))))
                .ToList();
            var returnType = new SimpleType(GetTypeName(method.ReturnType), GetAssemblyName(method.ReturnType));
            return MigrationMethod.TryParse(declaringType, parameters, returnType, method.Name);
        }

        private static string GetTypeName(Type type)
        {
            return type.GetTypeInfo().IsGenericType
                ? type.GetGenericTypeDefinition().FullName
                : type.GetTypeInfo().FullName;
        }

        private static AssemblyName GetAssemblyName(Type type)
        {
            return type.GetTypeInfo().Assembly.GetName();
        }

        public static IEnumerable<MigrationMethod> GetAndVerifyMigrationMethods(Type type)
        {
            var migrationMethodVerifier = new MigrationMethodVerifier(IsSuperType);

            var migrationMethods = ParseMigrationMethods(type.GetTypeInfo().DeclaredMethods);

            migrationMethods
                .Select(x => new { MigrationMethod = x, x.ReturnType })
                .StartWith(new { MigrationMethod = (MigrationMethod) null, ReturnType = (SimpleType) null })
                .Buffer(2, 1)
                .Where(l => l.Count == 2)
                .ForEach(l => migrationMethodVerifier.VerifyMigrationMethodSignature(l[1].MigrationMethod, l[0].ReturnType));

            return migrationMethods;
        }

        public static bool IsSuperType(SimpleType baseType, SimpleType derivedType)
        {
            var baseTypeInfo = Type.GetType(baseType.AssemblyQualifiedName).GetTypeInfo();
            var derivedTypeInfo = Type.GetType(derivedType.AssemblyQualifiedName).GetTypeInfo();
            return baseTypeInfo.IsAssignableFrom(derivedTypeInfo);
        }
    }
}