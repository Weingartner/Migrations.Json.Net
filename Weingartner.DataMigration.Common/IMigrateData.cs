using System;

namespace Weingartner.DataMigration.Common
{
    public interface IMigrateData<TData, in TType>
    {
        void Migrate(ref TData data, TType dataType);
    }
}