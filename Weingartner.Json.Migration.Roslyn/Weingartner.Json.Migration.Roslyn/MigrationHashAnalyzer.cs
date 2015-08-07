using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Weingartner.Json.Migration.Roslyn
{
    /// <summary>
    /// We just use this class for looking at the syntax
    /// tree of the Migratable attribute. Don't delete it
    /// </summary>
    [DataContract(Namespace = "XXX")]
    class Foo
    {
        [DataMember()]
        public string A { get; set; }
        public LocalizableString C { get; set; }
        public string B { get; set; }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MigrationHashAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MigrationHashAnalyzer";
        private static readonly LocalizableString Title = "Should have correct migration hash";
        public static readonly LocalizableString MessageFormat = "Expected migration hash of type '{0}' to be '{1}'.";

        private static readonly LocalizableString Description = "An incorrect migration hash is a hint that you may have forgotten to add a migration. " +
                                                                "The hash is calculated from all properties with a `DataMember` attribute " +
                                                                "and should be updated after all your migrations are written.";
        private const string Category = "DataMigration";

        private static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var migratableAttributeType = context.Compilation.GetTypeByMetadataName(Constants.MigratableAttributeMetadataName);
            var dataMemberAttributeType = context.Compilation.GetTypeByMetadataName(Constants.DataMemberAttributeMetadataName);
            if (migratableAttributeType != null && dataMemberAttributeType != null)
            {
                context.RegisterSyntaxNodeAction(nodeContext => AnalyzeCall(nodeContext, migratableAttributeType, dataMemberAttributeType), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
            }
        }

        // TODO create diagnostic if dataContractAttributeType is null
        // TODO create diagnostic if dataMemberAttributeType is null
        // TODO create diagnostic if dataContractAttribute is not set

        private static void AnalyzeCall(SyntaxNodeAnalysisContext context, ISymbol migratableAttributeType, ISymbol dataMemberAttributeType)
        {
            var ct = context.CancellationToken;
            var typeDeclaration = (TypeDeclarationSyntax) context.Node;
            var attribute = MigrationHashHelper.GetAttribute(typeDeclaration, migratableAttributeType, context.SemanticModel, ct);
            if (attribute == null) return;

            var attributeHash = GetAttributeHash(attribute, context.SemanticModel, ct);
            var computedHash = MigrationHashHelper.GetMigrationHashFromType(typeDeclaration, ct, context.SemanticModel, dataMemberAttributeType);

            if (attributeHash != computedHash)
            {
                var diagnostic = Diagnostic.Create(Rule, typeDeclaration.GetLocation(), typeDeclaration.Identifier.ToString(), computedHash, attributeHash);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static string GetAttributeHash(AttributeSyntax attribute, SemanticModel semanticModel, CancellationToken ct)
        {
            var hashNode = attribute?.ArgumentList?.Arguments.FirstOrDefault();
            if (hashNode == null) return null;

            var constantValue = semanticModel.GetConstantValue(hashNode.Expression, ct);
            return constantValue.HasValue ? (string)constantValue.Value : null;
        }
    }
}