using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration
{
    public class HashBasedDataMigrator<TSerializedData> : IMigrateData<TSerializedData>
        where TSerializedData : class
    {
        private readonly IUpdateVersions<TSerializedData> _VersionExtractor;

        public HashBasedDataMigrator(IUpdateVersions<TSerializedData> versionExtractor)
        {
            _VersionExtractor = versionExtractor;
        }

        public void TryMigrate(ref TSerializedData data, Type dataType)
        {
            TryMigrate(ref data, dataType, new SelfMigration<TSerializedData>(dataType));
        }

        public void TryMigrate(ref TSerializedData data, Type dataType, IMigrator<TSerializedData> migrator)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (migrator == null) throw new ArgumentNullException("migrator");

            if (dataType.GetCustomAttribute<MigratableAttribute>() == null) return;

            var currentVersion = GetCurrentVersion(dataType);
            var version = _VersionExtractor.GetVersion(data);

            while (version < currentVersion)
            {
                version++;
                migrator.MigrateData(ref data, version);
                _VersionExtractor.SetVersion(data, version);
            }
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
    }
}
