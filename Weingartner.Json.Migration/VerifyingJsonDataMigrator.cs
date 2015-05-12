using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration
{
    public class VerifyingJsonDataMigrator : IMigrateData<JToken>
    {
        private readonly IMigrateData<JToken> _Inner;

        public VerifyingJsonDataMigrator(IMigrateData<JToken> inner)
        {
            _Inner = inner;
        }

        public JToken TryMigrate(JToken data, Type dataType)
        {
            var migratedData = _Inner.TryMigrate(data, dataType);
            VerifyMigration(migratedData, dataType);
            return migratedData;
        }

        private static void VerifyMigration(JToken data, Type dataType)
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

            var roundTripProperties = JToken.FromObject(data.ToObject(dataType))
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
