using System;
using Newtonsoft.Json;

namespace Weingartner.Json.Migration
{
    public interface IMigrateData<TData>
    {
        TData TryMigrate(TData data, Type dataType, JsonSerializer serializer);
    }
}