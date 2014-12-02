using System;

namespace Weingartner.Json.Migration
{
    public interface IMigrateData<TSerializedData>
    {
        void TryMigrate(ref TSerializedData data, Type dataType);

        void TryMigrate(ref TSerializedData data, Type dataType, IMigrator<TSerializedData> migrator);
    }
}