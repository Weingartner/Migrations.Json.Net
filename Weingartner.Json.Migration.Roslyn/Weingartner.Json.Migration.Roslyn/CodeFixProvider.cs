using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WeingartnerJsonMigrationRoslynCodeFixProvider)), Shared]
    public class WeingartnerJsonMigrationRoslynCodeFixProvider : CodeFixProvider
    {
        private const string title = "Replace hash";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(WeingartnerJsonMigrationRoslynAnalyzer.DiagnosticId); }
        }

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
                    title: title,
                    createChangedSolution: c => FixMigrationHash(context.Document, declaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private async Task<Solution> FixMigrationHash(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var migrationHashCalculated = WeingartnerJsonMigrationRoslynAnalyzer.GetMigrationHashFromType(typeDecl);

            var node = CreateMigratableAttribute(migrationHashCalculated);

            var attr = WeingartnerJsonMigrationRoslynAnalyzer
                .MigratableAttributes(typeDecl)
                .First();

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(attr, node);
            return document.WithSyntaxRoot(newRoot).Project.Solution;
        }

        private AttributeSyntax CreateMigratableAttribute(string migrationHashCalculated)
        {
            return SyntaxFactory.Attribute(
                                        SyntaxFactory.IdentifierName(
                                            @"Migratable"))
                                    .WithArgumentList(
                                        SyntaxFactory.AttributeArgumentList(
                                            SyntaxFactory.SingletonSeparatedList<AttributeArgumentSyntax>(
                                                SyntaxFactory.AttributeArgument(
                                                    SyntaxFactory.LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        SyntaxFactory.Literal(
                                                            SyntaxFactory.TriviaList(),
                                                            $@"""{migrationHashCalculated}""",
                                                            $@"""{migrationHashCalculated}""",
                                                            SyntaxFactory.TriviaList())))))
                                        .WithOpenParenToken(
                                            SyntaxFactory.Token(
                                                SyntaxKind.OpenParenToken))
                                        .WithCloseParenToken(
                                            SyntaxFactory.Token(
                                                SyntaxKind.CloseParenToken)));
        }
    }
}