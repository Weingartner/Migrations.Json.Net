using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration.Roslyn
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MigrationMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MigrationMethodAnalyzer";
        internal static readonly LocalizableString Title = "Migration methods should have correct signature";
        public static readonly LocalizableString MessageFormat = "Invalid migration method '{0}' in type '{1}': {2}";
        internal const string Category = "DataMigration";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(OnCompilationStart);
        }
        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            var migratableAttributeType = context.Compilation.GetTypeByMetadataName(Constants.MigratableAttributeMetadataName);
            if (migratableAttributeType != null)
            {
                context.RegisterSyntaxNodeAction(nodeContext => AnalyzeCall(nodeContext, migratableAttributeType), SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
            }
        }

        private static void AnalyzeCall(SyntaxNodeAnalysisContext context, ISymbol migratableAttributeType)
        {
            var ct = context.CancellationToken;
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;
            var attribute = MigrationHashHelper.GetAttribute(typeDeclaration, migratableAttributeType, context.SemanticModel, ct);
            if (attribute == null) return;

            var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration, ct);
            var verifier = new MigrationMethodVerifier(CanAssign(context));
            var migrationMethods = MigrationHashHelper.GetMigrationMethods(typeSymbol);

            var invalidMethods = verifier.VerifyMigrationMethods(migrationMethods)
                .Where(x => x.Result != VerificationResultEnum.Ok);

            foreach (var x in invalidMethods)
            {
                var method = typeSymbol.GetMembers()
                    .First(sy => sy.Name == x.Method.Name);
                Debug.Assert(method.Locations.Length == 1, "Method has multiple locations.");
                var diagnostic = Diagnostic.Create(Rule, method.Locations[0], method.Name, typeSymbol.Name, x.Result);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static Func<SimpleType, SimpleType, bool> CanAssign(SyntaxNodeAnalysisContext context)
        {
            return (srcType, targetType) =>
            {
                Func<INamedTypeSymbol, IEnumerable<INamedTypeSymbol>> getBaseTypesAndSelf = null;
                getBaseTypesAndSelf = t =>
                {
                    if (t == null) return Enumerable.Empty<INamedTypeSymbol>();
                    return Enumerable.Repeat(t, 1).Concat(getBaseTypesAndSelf(t.BaseType));
                };

                var t1 = context.SemanticModel.Compilation.GetTypeByMetadataName(srcType.FullName);
                var t2 = context.SemanticModel.Compilation.GetTypeByMetadataName(targetType.FullName);
                return getBaseTypesAndSelf(t1).Contains(t2);
            };
        }
    }
}