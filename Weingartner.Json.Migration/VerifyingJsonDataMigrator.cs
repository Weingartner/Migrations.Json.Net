using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
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
            if (data is JArray)
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
                .Where(n => !VersionMemberName.SupportedVersionPropertyNames.Contains(n))
                .ToList();

            var dataMemberFilter =
                dataType.GetCustomAttribute<DataContractAttribute>() != null
                ? new Func<PropertyInfo, bool>(p => p.GetCustomAttribute<DataMemberAttribute>() != null)
                : (_ => true);

            var jsonObjectAttr = dataType.GetCustomAttribute<JsonObjectAttribute>();
            
            var jsonPropertyFilter =
                jsonObjectAttr != null && jsonObjectAttr.MemberSerialization == MemberSerialization.OptIn
                    ? new Func<PropertyInfo, bool>(p => p.GetCustomAttribute<JsonPropertyAttribute>() != null)
                    : (_ => true);

            var jsonIgnoreFilter =
                jsonObjectAttr != null && jsonObjectAttr.MemberSerialization != MemberSerialization.OptIn
                    ? new Func<PropertyInfo, bool>(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                    : (_ => true);

            var typeProperties = dataType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic && !VersionMemberName.SupportedVersionPropertyNames.Contains(p.Name))
                .Where(p => p.SetMethod != null) // No set method
                .Where(dataMemberFilter)
                .Where(jsonPropertyFilter)
                .Where(jsonIgnoreFilter)
                .Select(p => p.Name)
                .ToArray();

            var superfluousDataProperties = dataProperties.Except(typeProperties).ToList();
            if (superfluousDataProperties.Count > 0)
            {
                throw new MigrationException(string.Format("The following properties should be removed from the serialized data because they don't exist in type {0}: {1}", dataType.FullName, string.Join(", ", superfluousDataProperties)));
            }

            var missingDataProperties = typeProperties.Except(dataProperties).ToList();
            if (missingDataProperties.Count > 0)
            {
                throw new MigrationException(string.Format("The following properties should be added to the serialized data because they exist in type {0}: {1}", dataType.FullName, string.Join(", ", missingDataProperties)));
            }
        }
    }
}
