using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Newtonsoft.Json;

namespace Weingartner.Json.Migration.Common
{
    public static class VersionMemberName
    {
        public static string VersionPropertyName
        {
            get
            {
                return "Version";
            }
        }

        public static string VersionBackingFieldName
        {
            get
            {
                return "_version";
            }
        }

        public static IEnumerable<string> SupportedVersionPropertyNames
        {
            get
            {
                yield return VersionPropertyName;
                yield return "<>Version";
            }
        }

        private static IEnumerable<string> SupportedVersionBackingFieldNames
        {
            get
            {
                yield return VersionBackingFieldName;
                yield return "<>_version";
            }
        }

        public static int GetCurrentVersion(Type type)
        {


            var versionField = SupportedVersionBackingFieldNames
                .Select(n => type.GetField(n, BindingFlags.Static | BindingFlags.Public))
                .FirstOrDefault(x => x != null);
            if (versionField == null)
            {
                throw new MigrationException
                    (
                    String.Format
                        (
                         "Type '{0}' has no version field. " +
                         "Ensure that either the type has the custom attribute `[{1}]` and " +
                         "the NuGet package '{2}' is installed or add a public static field named '{3}'.",
                         type.FullName,
                         Regex.Replace(typeof (MigratableAttribute).Name, "Attribute$", String.Empty),
                         typeof (HashBasedDataMigrator<>).Assembly.GetName().Name,
                         VersionBackingFieldName));
            }
            return (int) versionField.GetValue(null);
        }

        private static void ThrowInvalidMigrationSignature<TData>(MethodInfo method) where TData : class
        {
            var builder = new StringBuilder();
            Debug.Assert(method.DeclaringType != null, "method.DeclaringType != null");
            builder.AppendLine
                (String.Format
                     (
                      "Migration method '{0}.{1}' should have the following signature:",
                      method.DeclaringType.FullName,
                      method.Name));
            builder.AppendLine
                (String.Format
                     ("private static {0} {1}({0} data, JsonSerializer serializer)",
                      typeof (TData).FullName,
                      method.Name));
            throw new MigrationException(builder.ToString());
        }

        public static void VerifyMigrationMethodSignature<TData>(MethodInfo method, Type dataType) where TData : class
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 2)
                ThrowInvalidMigrationSignature<TData>(method);

            if (!parameters[0].ParameterType.IsAssignableFrom(dataType))
                ThrowInvalidMigrationSignature<TData>(method);

            if (!typeof (JsonSerializer).IsAssignableFrom(parameters[1].ParameterType))
                ThrowInvalidMigrationSignature<TData>(method);

            if (!typeof (TData).IsAssignableFrom(method.ReturnType))
                ThrowInvalidMigrationSignature<TData>(method);
        }

        public static int MaxMigrationMethodVersionUsingCecil(TypeDefinition type)
        {
            return MigrationMethodVersionsUsingCecil(type).Concat(Enumerable.Repeat(0, 1)).Max();
        }

        public static IEnumerable<int> MigrationMethodVersionsUsingCecil(TypeDefinition type)
        {
            return type.Methods
                       .Where(m => m.IsStatic && m.IsPrivate)
                       .Where(m => m.Parameters.Count == 2)
                       .Select(m => Regex.Match(m.Name, @"(?<=^Migrate_)(\d+)$"))
                       .Where(m => m.Success)
                       .Select(m => Int32.Parse(m.Value));
        }
    }
}