using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

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

        /// <summary>
        /// Try to migrate the serialized data to a newer version using migration methods from type unserializedDataType.
        /// </summary>
        /// <param name="serializedData"></param>
        /// <param name="unserializedDataType"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public Tuple<TSerializedData,bool> TryMigrate(TSerializedData serializedData, Type unserializedDataType, JsonSerializer serializer)
        {
            if (serializedData == null) throw new ArgumentNullException(nameof(serializedData));
            if (unserializedDataType == null) throw new ArgumentNullException(nameof(unserializedDataType));

            var migrationSettings = unserializedDataType.GetTypeInfo().GetCustomAttribute<MigratableAttribute>();
            if (migrationSettings == null) return Tuple.Create(serializedData, false);


            var version = _VersionExtractor.GetVersion(serializedData);

            var allMigrationMethods = VersionMemberName
                .GetAndVerifyMigrationMethods(unserializedDataType)
                .ToList();

            var maxSupportedVersion = allMigrationMethods.LastOrDefault()?.ToVersion ?? 0;

            if ( version > maxSupportedVersion )
            {
                throw new DataVersionTooHighException($"Trying to load data type '{unserializedDataType.FullName}' from json data " +
                                                      $"at version {version}." +
                                                      $" However current software only supports version {maxSupportedVersion}." +
                                                      " Please update your installation with a newwer version.");
            }

            var migrationMethods = allMigrationMethods
                .SkipWhile(m => m.ToVersion <= version)
                .ToList();

            var migrated = migrationMethods.Count > 0;
            
            serializedData = migrationMethods
                .Select(m => unserializedDataType.GetTypeInfo().GetDeclaredMethod(m.Name))
                .Aggregate(serializedData, (data, method) => ExecuteMigration(method, data, serializer));

            _VersionExtractor.SetVersion(serializedData, maxSupportedVersion);

            return Tuple.Create(serializedData, migrated);
        }

        protected TSerializedData ExecuteMigration(MethodInfo method, TSerializedData data, JsonSerializer serializer)
        {
            return (TSerializedData)method.Invoke(null, new object[] { data, serializer });
        }
    }
}
