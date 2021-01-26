using System;
using Newtonsoft.Json;

namespace Weingartner.Json.Migration
{
    public interface IMigrateData<TData>
    {
        /// <summary>
        /// Tries to migrate the data.
        /// </summary>
        /// <param name="serializedData"></param>
        /// <param name="unserializedDataType"></param>
        /// <param name="serializer"></param>
        /// <returns>The migrated data and true if the data was modified otherwise false</returns>
        Tuple<TData,bool> TryMigrate(TData serializedData, Type unserializedDataType, JsonSerializer serializer);
    }
}