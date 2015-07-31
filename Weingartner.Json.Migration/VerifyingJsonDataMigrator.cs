using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration
{
    public class VerifyingJsonDataMigrator : IMigrateData<JToken>
    {
        private readonly IMigrateData<JToken> _Inner;

        public bool IsVerifying { private set; get; }

        public VerifyingJsonDataMigrator(IMigrateData<JToken> inner)
        {
            _Inner = inner;
        }

        public JToken TryMigrate(JToken serializedData, Type unserializedDataType, JsonSerializer serializer)
        {
            var migratedData = _Inner.TryMigrate(serializedData, unserializedDataType, serializer);
            if (!IsVerifying)
            {
                try
                {
                    IsVerifying = true;
                    VerifyMigration(migratedData, unserializedDataType, serializer);
                }
                finally
                {
                    IsVerifying = false;
                }
            }
            return migratedData;
        }

        private void VerifyMigration(JToken data, Type dataType, JsonSerializer jsonSerializer)
        {
            var dataArr = data as JArray;
            if (dataArr != null)
            {
                if (!dataType.GetTypeInfo().IsGenericType)
                {
                    // We can't verify anything if we don't know the type
                    return;
                }
                // Let's assume that the first generic argument refers to the element type
                var childType = dataType.GenericTypeArguments.First();
                foreach (var child in data)
                {
                    VerifyMigration(child, childType, jsonSerializer);
                }
                return;
            }

            var dataProperties = data
                .Children<JProperty>()
                .Select(p => p.Name)
                .ToList();

            var serialized = data.ToObject(dataType, jsonSerializer);
            var deserialized = serialized != null ? JToken.FromObject(serialized, jsonSerializer ) : new JObject();
            var roundTripProperties = deserialized
                .Children<JProperty>()
                .Select(p => p.Name)
                .ToList();

            var ignoredMembers = VersionMemberName.SupportedVersionPropertyNames.ToList();
            var superfluousDataProperties = dataProperties.Except(ignoredMembers).Except(roundTripProperties).ToList();
            if (superfluousDataProperties.Count > 0)
            {
                throw new MigrationException(
                    "The following properties should be removed from the serialized data because they don't exist in type " +
                    $"{dataType.FullName}: {string.Join(", ", superfluousDataProperties)}");
            }

            var missingDataProperties = roundTripProperties.Except(ignoredMembers).Except(dataProperties).ToList();
            if (missingDataProperties.Count > 0)
            {
                throw new MigrationException(
                    "The following properties should be added to the serialized data because they exist in type " +
                    $"{dataType.FullName}: {string.Join(", ", missingDataProperties)}");
            }
        }
    }
}
