using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration
{
    public class HashBasedDataMigrator<TData> : IMigrateData<TData>
        where TData : class
    {
        private readonly IUpdateVersions<TData> _VersionExtractor;

        public HashBasedDataMigrator(IUpdateVersions<TData> versionExtractor)
        {
            _VersionExtractor = versionExtractor;
        }

        public TData TryMigrate(TData data, Type dataType)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (dataType == null) throw new ArgumentNullException("dataType");

            var migrationSettings = dataType.GetCustomAttribute<MigratableAttribute>();
            if (migrationSettings == null) return data;

            var migrator = migrationSettings.MigratorType ?? dataType;

            var currentVersion = GetCurrentVersion(dataType);
            var version = _VersionExtractor.GetVersion(data);

            while (version < currentVersion)
            {
                var migrationMethod = GetMigrationMethod(migrator, version + 1);
                if (migrationMethod == null)
                {
                    throw new MigrationException(
                        string.Format(
                            "The migration method, which migrates an instance of type '{0}' to version {1} cannot be found. " +
                            "To resolve this, add a method with the following signature: `private static {2} Migrate_{1}(ref {2} data)",
                            dataType.FullName,
                            version + 1,
                            typeof(TData).FullName));
                }

                VerifyMigrationMethodSignature(migrationMethod, data.GetType());

                data = ExecuteMigration(migrationMethod, data);

                version++;

                _VersionExtractor.SetVersion(data, version);
            }

            return data;
        }

        protected int GetCurrentVersion(Type type)
        {
            var versionField = VersionMemberName.SupportedVersionBackingFieldNames
                .Select(n => type.GetField(n, BindingFlags.Static | BindingFlags.NonPublic))
                .FirstOrDefault(x => x != null);
            if (versionField == null)
            {
                throw new MigrationException(
                    string.Format(
                        "Type '{0}' has no version field. " +
                        "Ensure that either the type has the custom attribute `[{1}]` and " +
                        "the NuGet package '{2}' is installed or add a public static field named '{3}'.",
                        type.FullName,
                        Regex.Replace(typeof(MigratableAttribute).Name, "Attribute$", string.Empty),
                        typeof(HashBasedDataMigrator<>).Assembly.GetName().Name,
                        VersionMemberName.VersionBackingFieldName));
            }
            return (int)versionField.GetValue(null);
        }

        protected MethodInfo GetMigrationMethod(Type type, int version)
        {
            return type.GetMethod("Migrate_" + version, BindingFlags.Static | BindingFlags.NonPublic);
        }

        protected void VerifyMigrationMethodSignature(MethodInfo method, Type dataType)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
                ThrowInvalidMigrationSignature(method);

            if (!parameters[0].ParameterType.IsAssignableFrom(dataType))
                ThrowInvalidMigrationSignature(method);

            if (!typeof(TData).IsAssignableFrom(method.ReturnType))
                ThrowInvalidMigrationSignature(method);
        }

        private static void ThrowInvalidMigrationSignature(MethodInfo method)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Format(
                "Migration method '{0}.{1}' should have the following signature:",
                method.DeclaringType.FullName,
                method.Name));
            builder.AppendLine(string.Format("private static {0} {1}({0} data)", typeof(TData).FullName, method.Name));
            throw new MigrationException(builder.ToString());
        }

        protected TData ExecuteMigration(MethodInfo method, TData data)
        {
            return (TData)method.Invoke(null, new object[] { data });
        }
    }
}
