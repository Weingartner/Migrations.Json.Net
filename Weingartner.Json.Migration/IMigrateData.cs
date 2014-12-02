using System;

namespace Weingartner.Json.Migration
{
    public interface IMigrateData<TData>
    {
        void TryMigrate(ref TData data, Type dataType);
    }
}