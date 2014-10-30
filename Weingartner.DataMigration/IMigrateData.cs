using System;

namespace Weingartner.DataMigration
{
    public interface IMigrateData<TData>
    {
        void Migrate(ref TData data, Type dataType);
    }
}