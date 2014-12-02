namespace Weingartner.Json.Migration
{
    public interface IMigrator<TSerializedData>
    {
        void MigrateData(ref TSerializedData data, int toVersion);
    }
}