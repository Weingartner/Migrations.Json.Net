using System;
using Newtonsoft.Json;

namespace Weingartner.Json.Migration
{
    public interface IMigrateData<TData>
    {
        TData TryMigrate(TData serializedData, Type unserializedDataType, JsonSerializer serializer);
    }
}