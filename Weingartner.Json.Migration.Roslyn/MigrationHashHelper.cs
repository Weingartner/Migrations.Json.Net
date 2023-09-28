using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration.Roslyn
{
    public static class MigrationHashHelper
    {
        public static string GetMigrationHashFromType(TypeDeclarationSyntax typeDeclaration, CancellationToken ct, SemanticModel semanticModel, ISymbol dataMemberAttributeType)
        {
            var properties = GetDataMembers(typeDeclaration, ct, semanticModel, dataMemberAttributeType);
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

        public static ImmutableList<DataMember> GetDataMembers(
            TypeDeclarationSyntax typeDeclaration,
            CancellationToken ct,
            SemanticModel semanticModel,
            ISymbol dataMemberAttributeType)
        {
            Func<SyntaxList<AttributeListSyntax>, bool> isDataMember = lists =>
            {
                return lists
                    .SelectMany(l => l.Attributes)
                    .Any(a =>
                        semanticModel.GetTypeInfo(a, ct).Type.MetadataName ==
                        dataMemberAttributeType.MetadataName
                    );
            };

            var properties = typeDeclaration
                .Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => isDataMember(p.AttributeLists))
                .Select(p => new DataMember(p.Type.ToString(), p.Identifier.ToString()));

            var fields = typeDeclaration
                .Members
                .OfType<FieldDeclarationSyntax>()
                .Where(p => isDataMember(p.AttributeLists))
                .SelectMany(p => p.Declaration.Variables
                    .Select(v => new DataMember(p.Declaration.Type.ToString(), v.Identifier.ToString()))
                );

            // primary constructor in records using targeted attributes
            var primary = typeDeclaration is RecordDeclarationSyntax recordDeclaration
                ? recordDeclaration.ParameterList != null
                    ? recordDeclaration.ParameterList
                        .Parameters
                        .Where( p => isDataMember( p.AttributeLists ) )
                        .Select( p => new DataMember( p.Type.ToString(), p.Identifier.ToString() ) )
                    : Enumerable.Empty<DataMember>()
                : Enumerable.Empty<DataMember>();

            return properties
                .Concat(fields)
                .Concat(primary)
                .OrderBy(p => p.Type)
                .ThenBy(p => p.Identifier)
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
                    return typeSymbol?.Equals(attributeType, SymbolEqualityComparer.Default) ?? false;
                });
        }

        public static IReadOnlyList<MigrationMethod> GetMigrationMethods(ITypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Select(m =>
                {
                    var declaringType = new SimpleType(m.ContainingType.ToString(),
                        new AssemblyName(m.ContainingType.ContainingAssembly.ToString()));
                    var parameters = m.Parameters
                        .Select(p =>
                        {
                            var parameterTypeAssemblyName = p.Type.Kind == SymbolKind.ArrayType 
                                ? ((IArrayTypeSymbol)p.Type).ElementType.ContainingAssembly.ToString()
                                : p.Type.ContainingAssembly.ToString();
                            var parameterType = new SimpleType(p.Type.ToString(),
                                new AssemblyName(parameterTypeAssemblyName));
                            return new MethodParameter(parameterType);
                        })
                        .ToList();
                    var returnTypeAssemblyName = m.ReturnType.Kind == SymbolKind.ArrayType
                        ? ((IArrayTypeSymbol)m.ReturnType).ElementType.ContainingAssembly.ToString()
                        : m.ReturnType.ContainingAssembly.ToString();
                    var returnType = new SimpleType(m.ReturnType.ToString(), new AssemblyName(returnTypeAssemblyName));
                    return MigrationMethod.TryParse(declaringType, parameters, returnType, m.Name);
                })
                .Where(m => m != null)
                .OrderBy(m => m.ToVersion)
                .ToList();
        }

        public static TypeDeclarationSyntax UpdateMigrationHash(TypeDeclarationSyntax typeDecl, CancellationToken ct,
            SemanticModel semanticModel)
        {
            var dataMemberAttributeType =
                semanticModel.Compilation.GetTypeByMetadataName(Constants.DataMemberAttributeMetadataName);
            var migratableAttributeType =
                semanticModel.Compilation.GetTypeByMetadataName(Constants.MigratableAttributeMetadataName);

            var migrationHashCalculated = GetMigrationHashFromType(typeDecl, ct, semanticModel,
                dataMemberAttributeType);

            var node = CreateMigratableAttribute(migratableAttributeType, migrationHashCalculated);

            var attr = GetAttribute(typeDecl, migratableAttributeType, semanticModel, ct);
            return typeDecl.ReplaceNode(attr, node);
        }

        private static AttributeSyntax CreateMigratableAttribute(ISymbol migratableAttributeType, string migrationHashCalculated)
        {
            return SyntaxFactory
                .Attribute(SyntaxFactory.IdentifierName(Regex.Replace(migratableAttributeType.Name, "Attribute$", "")))
                .WithArgumentList(SyntaxFactory.ParseAttributeArgumentList($@"(""{migrationHashCalculated}"")"));
        }
    }

    public class DataMember
    {
        public DataMember(string type, string identifier)
        {
            Type = type;
            Identifier = identifier;
        }

        public string Type { get; }
        public string Identifier { get; }
    }
}
