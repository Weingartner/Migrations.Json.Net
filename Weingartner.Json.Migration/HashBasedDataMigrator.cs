using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
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

        public TData TryMigrate(TData data, Type dataType, JsonSerializer serializer)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (dataType == null) throw new ArgumentNullException("dataType");

            var migrationSettings = dataType.GetCustomAttribute<MigratableAttribute>();
            if (migrationSettings == null) return data;

            var migrator = migrationSettings.MigratorType ?? dataType;

            var currentVersion = VersionMemberName.GetCurrentVersion(dataType);
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

               VersionMemberName.VerifyMigrationMethodSignature<TData>(migrationMethod, data.GetType());

                data = ExecuteMigration(migrationMethod, data, serializer);

                version++;

                _VersionExtractor.SetVersion(data, version);
            }

            if (version > currentVersion)
            {
                throw new DataVersionTooHighException("Can't migrate data of type {0} because there is no migration method available.");
            }

            return data;
        }

        protected MethodInfo GetMigrationMethod(Type type, int version)
        {
            return type.GetMethod("Migrate_" + version, BindingFlags.Static | BindingFlags.NonPublic);
        }

        protected TData ExecuteMigration(MethodInfo method, TData data, JsonSerializer serializer)
        {
            return (TData)method.Invoke(null, new object[] { data, serializer });
        }
    }
}
