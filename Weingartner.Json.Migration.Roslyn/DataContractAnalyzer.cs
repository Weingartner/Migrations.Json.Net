using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Weingartner.Json.Migration.Roslyn
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DataContractAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DataContractAnalyzer";
        private static readonly LocalizableString Title = "Migratable type should have `DataContract` and `DataMember` attributes";
        public static readonly LocalizableString MessageFormat = "Type '{0}' is migratable but is missing either `DataContract` or `DataMember` attributes";
        private const string Category = "DataMigration";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var migratableAttributeType = context.Compilation.GetTypeByMetadataName(Constants.MigratableAttributeMetadataName);
            var dataContractAttributeType = context.Compilation.GetTypeByMetadataName(Constants.DataContractAttributeMetadataName);
            var dataMemberAttributeType = context.Compilation.GetTypeByMetadataName(Constants.DataMemberAttributeMetadataName);
            if (migratableAttributeType != null)
            {
                context.RegisterSyntaxNodeAction(nodeContext => AnalyzeCall(nodeContext, migratableAttributeType, dataContractAttributeType, dataMemberAttributeType), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration);
            }
        }

        private static void AnalyzeCall(SyntaxNodeAnalysisContext context, ISymbol migratableAttributeType, ISymbol dataContractAttributeType, ISymbol dataMemberAttributeType)
        {
            var typeDecl = (TypeDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var ct = context.CancellationToken;

            var isMigratable = MigrationHashHelper.HasAttribute(typeDecl, migratableAttributeType, semanticModel, ct);
            if (!isMigratable) return;

            if (dataContractAttributeType != null && dataMemberAttributeType != null)
            {
                var isDataContract = MigrationHashHelper.HasAttribute(typeDecl, dataContractAttributeType, semanticModel, ct);
                var hasDataMember = MigrationHashHelper.GetDataMembers(typeDecl, ct, semanticModel, dataMemberAttributeType).Any();

                if (isDataContract && hasDataMember) return;
            }

            var diagnostic = Diagnostic.Create(Rule, typeDecl.GetLocation(), typeDecl.Identifier.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}