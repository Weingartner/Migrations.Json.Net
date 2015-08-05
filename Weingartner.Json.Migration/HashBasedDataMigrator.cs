using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
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

        /// <summary>
        /// Try to migrate the serialized data to a newer version using migration methods from type unserializedDataType.
        /// </summary>
        /// <param name="serializedData"></param>
        /// <param name="unserializedDataType"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public TSerializedData TryMigrate(TSerializedData serializedData, Type unserializedDataType, JsonSerializer serializer)
        {
            if (serializedData == null) throw new ArgumentNullException(nameof(serializedData));
            if (unserializedDataType == null) throw new ArgumentNullException(nameof(unserializedDataType));

            var migrationSettings = unserializedDataType.GetTypeInfo().GetCustomAttribute<MigratableAttribute>();
            if (migrationSettings == null) return serializedData;


            var version = _VersionExtractor.GetVersion(serializedData);

            var migrationMethodCandidates = VersionMemberName
                .GetMigrationMethodCandidates(unserializedDataType)
                .ToList();

            if(migrationMethodCandidates.Count == 0)
                return serializedData;

            var maxSupportedVersion = migrationMethodCandidates.Last().Version;

            if ( version > maxSupportedVersion )
            {
                throw new DataVersionTooHighException($"Trying to load data type '{unserializedDataType.FullName}' from json data " +
                                                      $"at version {version}." +
                                                      $" However current software only supports version {maxSupportedVersion}." +
                                                      " Please update your installation with a newwer version.");
            }

            var migrationMethods = migrationMethodCandidates
                .SkipWhile(m => m.Version <= version)
                .ToList();
            
            serializedData = migrationMethods
                .Aggregate(serializedData, (data, method) => ExecuteMigration(method.Method, data, serializer));

            _VersionExtractor.SetVersion(serializedData, maxSupportedVersion);

            return serializedData;
        }

        protected TSerializedData ExecuteMigration(MethodInfo method, TSerializedData data, JsonSerializer serializer)
        {
            return (TSerializedData)method.Invoke(null, new object[] { data, serializer });
        }
    }
}
