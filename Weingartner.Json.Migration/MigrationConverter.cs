using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Weingartner.Json.Migration
{
    public class MigrationConverter : JsonConverter
    {
        protected IMigrateData<JToken> DataMigrator;

        readonly ThreadLocal<Dictionary<Type, bool>> _MigratedTypes =
            new ThreadLocal<Dictionary<Type, bool>>(() => new Dictionary<Type, bool>());

        public MigrationConverter(IMigrateData<JToken> dataMigrator)
        {
            DataMigrator = dataMigrator;
        }

        public static bool GetOrDefault
            (Dictionary<Type, bool> @this, Type type, bool def)
        {
            bool value;
            return @this.TryGetValue(type, out value) ? value : def;
        }

        public override bool CanConvert(Type objectType)
        {
            var isMigrated = GetOrDefault(_MigratedTypes.Value, objectType, false);
            var isMigratable = objectType.GetTypeInfo().GetCustomAttribute<MigratableAttribute>() != null;
            return !isMigrated && isMigratable;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var data = JToken.Load(reader);
            var migratedData = DataMigrator.TryMigrate(data, objectType, serializer );

            try
            {
                _MigratedTypes.Value[objectType] = true;
                return serializer.Deserialize(migratedData.CreateReader(), objectType);
            }
            finally
            {
                _MigratedTypes.Value[objectType] = false;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var objectType = value.GetType();
            try
            {
                _MigratedTypes.Value[objectType] = true;
                var obj = JObject.FromObject(value, serializer);
                obj[VersionMemberName.VersionPropertyName] = VersionMemberName.GetCurrentVersion(objectType);
                obj.WriteTo(writer);
            }
            finally
            {
                _MigratedTypes.Value[objectType] = false;
            }
        }
    }
}
