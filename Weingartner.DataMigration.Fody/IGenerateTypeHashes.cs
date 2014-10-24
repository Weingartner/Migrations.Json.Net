using Mono.Cecil;

namespace Weingartner.DataMigration.Fody
{
    public interface IGenerateTypeHashes
    {
        string GenerateHash(TypeDefinition type);
    }
}