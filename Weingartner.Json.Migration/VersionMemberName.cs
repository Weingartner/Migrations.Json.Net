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
            return methods
                .Select(GetMigrationMethod)
                .Where(x => x != null)
                .OrderBy(m => m.ToVersion)
                .ToList();
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
            var migrationMethodVerifier = new MigrationMethodVerifier(CanAssign);

            var migrationMethods = ParseMigrationMethods(type.GetTypeInfo().DeclaredMethods);
            var invalidMethod = migrationMethodVerifier.VerifyMigrationMethods(migrationMethods);
            foreach (var method in invalidMethod)
            {
                method.ThrowIfInvalid();
            }

            return migrationMethods;
        }

        public static bool CanAssign(SimpleType srcType, SimpleType targetType)
        {
            var srcTypeInfo = Type.GetType(srcType.AssemblyQualifiedName).GetTypeInfo();
            var targetTypeInfo = Type.GetType(targetType.AssemblyQualifiedName).GetTypeInfo();
            return targetTypeInfo.IsAssignableFrom(srcTypeInfo);
        }
    }
}