using System;
using System.Reflection;
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

        public void TryMigrate(ref TData data, Type dataType)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (dataType == null) throw new ArgumentNullException("dataType");

            if (dataType.GetCustomAttribute<MigratableAttribute>() == null) return;

            var currentVersion = GetCurrentVersion(dataType);
            var version = _VersionExtractor.GetVersion(data);

            while (version < currentVersion)
            {
                var migrationMethod = GetMigrationMethod(dataType, version);
                if (migrationMethod == null)
                {
                    throw new MigrationException(
                        string.Format(
                            "The migration method, which migrates an instance of type '{0}' to version {1} cannot be found. " +
                            "To resolve this, add a method with the following signature: `private static void Migrate_{1}(ref {2} data)",
                            dataType.FullName,
                            version,
                            typeof(TData).FullName));
                }

                CheckParameters(migrationMethod, data.GetType());

                ExecuteMigration(migrationMethod, ref data);

                version++;

                _VersionExtractor.SetVersion(data, version);
            }
        }

        protected void ThrowInvalidParametersException(string typeName, string methodName)
        {
            throw new MigrationException(
                string.Format(
                    "Migration method '{0}.{1}' must have a single parameter of type '{2}'.",
                    typeName,
                    methodName,
                    typeof(TData).FullName));
        }

        protected int GetCurrentVersion(Type type)
        {
            var versionField = type.GetField(VersionMemberName.Instance.VersionBackingFieldName, BindingFlags.Static | BindingFlags.NonPublic);
            if (versionField == null)
            {
                throw new MigrationException(
                    string.Format(
                        "Type '{0}' has no version field. " +
                        "Ensure that either the type has the custom attribute `[{1}]` and " +
                        "the NuGet package '{2}' is installed or add a private static field named '{3}'.",
                        type.FullName,
                        Regex.Replace(typeof(MigratableAttribute).Name, "Attribute$", string.Empty),
                        typeof(HashBasedDataMigrator<>).Assembly.GetName().Name,
                        VersionMemberName.Instance.VersionBackingFieldName));
            }
            return (int)versionField.GetValue(null);
        }

        protected MethodInfo GetMigrationMethod(Type type, int version)
        {
            return type.GetMethod("Migrate_" + version, BindingFlags.Static | BindingFlags.NonPublic);
        }

        protected void CheckParameters(MethodInfo method, Type dataType)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1 || !parameters[0].ParameterType.IsAssignableFrom(dataType.MakeByRefType()))
            {
                // ReSharper disable once PossibleNullReferenceException
                ThrowInvalidParametersException(method.DeclaringType.FullName, method.Name);
            }
        }

        protected void ExecuteMigration(MethodInfo method, ref TData data)
        {
            var parameters = new object[] { data };
            method.Invoke(null, parameters);
            data = (TData)parameters[0];
        }
    }
}
