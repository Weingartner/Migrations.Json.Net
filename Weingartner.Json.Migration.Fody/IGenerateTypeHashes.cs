using Mono.Cecil;

namespace Weingartner.Json.Migration.Fody
{
    public interface IGenerateTypeHashes
    {
        string GenerateHash(TypeDefinition type);
    }
}