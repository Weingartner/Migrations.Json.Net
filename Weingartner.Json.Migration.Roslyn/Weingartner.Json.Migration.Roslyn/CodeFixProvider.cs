using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Weingartner.Json.Migration.Roslyn
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MigrationHashAnalyzerCodeFixProvider)), Shared]
    public class MigrationHashAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Correct migration hash";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(MigrationHashAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => FixMigrationHash(context.Document, declaration, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> FixMigrationHash(Document document, TypeDeclarationSyntax typeDecl, CancellationToken ct)
        {
            var semanticModel = await document.GetSemanticModelAsync(ct);
            var dataMemberAttributeType =
                semanticModel.Compilation.GetTypeByMetadataName(Constants.DataMemberAttributeMetadataName);
            var migratableAttributeType =
                semanticModel.Compilation.GetTypeByMetadataName(Constants.MigratableAttributeMetadataName);

            var migrationHashCalculated = MigrationHashHelper.GetMigrationHashFromType(typeDecl, ct, semanticModel, dataMemberAttributeType);

            var node = CreateMigratableAttribute(migratableAttributeType, migrationHashCalculated);

            var attr = MigrationHashHelper.GetAttribute(typeDecl, migratableAttributeType, semanticModel, ct);

            var root = await document.GetSyntaxRootAsync(ct);
            var newRoot = root.ReplaceNode(attr, node);
            return document.WithSyntaxRoot(newRoot);
        }

        private static AttributeSyntax CreateMigratableAttribute(ISymbol migratableAttributeType, string migrationHashCalculated)
        {
            return SyntaxFactory
                .Attribute(SyntaxFactory.IdentifierName(Regex.Replace(migratableAttributeType.Name, "Attribute$", "")))
                .WithArgumentList(SyntaxFactory.ParseAttributeArgumentList($@"(""{migrationHashCalculated}"")"));
        }
    }
}