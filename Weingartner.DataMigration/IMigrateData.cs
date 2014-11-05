using System;

namespace Weingartner.DataMigration
{
    public interface IMigrateData<TData>
    {
        void TryMigrate(ref TData data, Type dataType);
    }
}