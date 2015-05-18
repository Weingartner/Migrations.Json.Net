using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration
{
    public class VerifyingJsonDataMigrator : IMigrateData<JToken>
    {
        private readonly IMigrateData<JToken> _Inner;
        private readonly Func<JsonSerializer> _Serializer;

        public bool IsVerifying { private set; get; }

        public VerifyingJsonDataMigrator(IMigrateData<JToken> inner, Func<JsonSerializer> serializer)
        {
            _Inner = inner;
            _Serializer = serializer;
        }

        public JToken TryMigrate(JToken data, Type dataType)
        {
            var migratedData = _Inner.TryMigrate(data, dataType);
            if (!IsVerifying)
            {
                try
                {
                    IsVerifying = true;
                    VerifyMigration(migratedData, dataType);
                }
                finally
                {
                    IsVerifying = false;
                }
            }
            return migratedData;
        }

        private void VerifyMigration(JToken data, Type dataType)
        {
            var dataArr = data as JArray;
            if (dataArr != null)
            {
                if (!dataType.IsGenericType)
                {
                    // We can't verify anything if we don't know the type
                    return;
                }
                // Let's assume that the first generic argument refers to the element type
                var childType = dataType.GetGenericArguments().First();
                foreach (var child in data)
                {
                    VerifyMigration(child, childType);
                }
                return;
            }

            var dataProperties = data
                .Children<JProperty>()
                .Select(p => p.Name)
                .ToList();

            var serializer = _Serializer();
            var roundTripProperties = JToken.FromObject(data.ToObject(dataType, serializer), serializer)
                .Children<JProperty>()
                .Select(p => p.Name)
                .ToList();

            var superfluousDataProperties = dataProperties.Except(roundTripProperties).ToList();
            if (superfluousDataProperties.Count > 0)
            {
                throw new MigrationException(string.Format("The following properties should be removed from the serialized data because they don't exist in type {0}: {1}", dataType.FullName, string.Join(", ", superfluousDataProperties)));
            }

            var missingDataProperties = roundTripProperties.Except(dataProperties.Concat(VersionMemberName.SupportedVersionPropertyNames)).ToList();
            if (missingDataProperties.Count > 0)
            {
                throw new MigrationException(string.Format("The following properties should be added to the serialized data because they exist in type {0}: {1}", dataType.FullName, string.Join(", ", missingDataProperties)));
            }
        }
    }
}
