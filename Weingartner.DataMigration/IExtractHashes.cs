namespace Weingartner.DataMigration
{
    public interface IExtractHashes<in T>
    {
        string ExtractHash(T data);
    }
}
