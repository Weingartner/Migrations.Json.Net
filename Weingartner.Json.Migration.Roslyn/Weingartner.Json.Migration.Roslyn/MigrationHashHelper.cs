using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Weingartner.Json.Migration.Roslyn
{
    public static class MigrationHashHelper
    {
        public static string GetMigrationHashFromType(TypeDeclarationSyntax typeDeclaration, CancellationToken ct, SemanticModel semanticModel, ISymbol dataMemberAttributeType)
        {
            var properties = GetDataMemberProperties(typeDeclaration, ct, semanticModel, dataMemberAttributeType);
            var identifier = string.Join(";", properties.Select(p => p.Type.ToString() + "|" + p.Identifier.ToString()));
            return Hash(identifier);
        }

        private static string Hash(string text)
        {
            unchecked
            {
                return text
                    .Cast<char>()
                    .Aggregate(23, (current, c) => current * 31 + c)
                    .ToString();
            }
        }

        private static IImmutableList<PropertyDeclarationSyntax> GetDataMemberProperties(TypeDeclarationSyntax typeDeclaration, CancellationToken ct, SemanticModel semanticModel, ISymbol dataMemberAttributeType)
        {
            return typeDeclaration
                .ChildNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(p =>
                    p.AttributeLists
                    .SelectMany(l => l.Attributes)
                    .Any(a => (semanticModel.GetTypeInfo(a, ct).Type.MetadataName == dataMemberAttributeType.MetadataName))
                )
                .OrderBy(p => p.Identifier.ToString())
                .ToImmutableList();
        }

        public static bool HasAttribute(TypeDeclarationSyntax node, ISymbol attributeType, SemanticModel semanticModel, CancellationToken ct)
        {
            return GetAttribute(node, attributeType, semanticModel, ct) != null;
        }

        public static bool HasAttribute(PropertyDeclarationSyntax node, ISymbol attributeType, SemanticModel semanticModel, CancellationToken ct)
        {
            return GetAttribute(node.AttributeLists, attributeType, semanticModel, ct) != null;
        }

        public static AttributeSyntax GetAttribute(TypeDeclarationSyntax typeDecl, ISymbol attributeType, SemanticModel semanticModel, CancellationToken ct)
        {
            return GetAttribute(typeDecl.AttributeLists, attributeType, semanticModel, ct);
        }

        private static AttributeSyntax GetAttribute(SyntaxList<AttributeListSyntax> attributeLists, ISymbol attributeType, SemanticModel semanticModel, CancellationToken ct)
        {
            return attributeLists
                .SelectMany(l => l.Attributes)
                .FirstOrDefault(a =>
                {
                    var ctorSymbol = semanticModel.GetSymbolInfo(a, ct).Symbol;
                    var typeSymbol = ctorSymbol?.ContainingSymbol;
                    return typeSymbol?.Equals(attributeType) ?? false;
                });
        }
    }
}
