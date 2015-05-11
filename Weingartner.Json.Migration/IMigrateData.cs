using System;

namespace Weingartner.Json.Migration
{
    public interface IMigrateData<TData>
    {
        TData TryMigrate(TData data, Type dataType);
    }
}