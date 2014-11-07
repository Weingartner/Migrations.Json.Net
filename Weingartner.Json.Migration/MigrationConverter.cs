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

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public static bool GetOrDefault
            (Dictionary<Type, bool> @this, Type type, bool def)
        {
            bool value;
            return @this.TryGetValue(type, out value) ? value : def;
        }

        public override bool CanConvert(Type objectType)
        {
            return !GetOrDefault(_MigratedTypes.Value, objectType, false)
                && objectType.GetCustomAttribute<MigratableAttribute>() != null;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var data = JToken.Load(reader);
            DataMigrator.TryMigrate(ref data, objectType);

            try
            {
                _MigratedTypes.Value[objectType] = true;
                return serializer.Deserialize(data.CreateReader(), objectType);
            }
            finally
            {
                _MigratedTypes.Value[objectType] = false;
            }
        }
    }
}
