using System;
using System.Composition;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using Document = Microsoft.CodeAnalysis.Document;
using Formatter = Microsoft.CodeAnalysis.Formatting.Formatter;

namespace Weingartner.Json.Migration.Roslyn
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddMigrationMethodCodeFixProvider)), Shared]
    public class AddMigrationMethodCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add migration method";

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
                    createChangedDocument: c => AddMigrationMethod(context.Document, declaration, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> AddMigrationMethod(Document document, TypeDeclarationSyntax typeDecl, CancellationToken ct)
        {
            var semanticModel = await document.GetSemanticModelAsync(ct);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, ct);
            var latestMigrationMethod = MigrationHashHelper.GetMigrationMethods(typeSymbol).LastOrDefault();

            var latestVersion = latestMigrationMethod?.ToVersion ?? 0;
            var dataArgumentTypeName = latestMigrationMethod?.ReturnType.Name ?? "JToken";
            var method = GetMigrationMethod(latestVersion + 1, dataArgumentTypeName, ct);
            var typeDeclWithUpdatedMigrationHash = MigrationHashHelper.UpdateMigrationHash(typeDecl, ct, semanticModel);
            var typeDeclWithAddedMigrationMethod = AddMember(typeDeclWithUpdatedMigrationHash, method);

            var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(ct);
            var rootWithNewTypeDecl = root.ReplaceNode(typeDecl, typeDeclWithAddedMigrationMethod);

            return document.WithSyntaxRoot(rootWithNewTypeDecl);
        }

        private static TypeDeclarationSyntax AddMember(TypeDeclarationSyntax node, MemberDeclarationSyntax member)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)node).AddMembers(member);
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)node).AddMembers(member);
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)node).AddMembers(member);
            }
            // Impossible
            return null;
        }

        private static MethodDeclarationSyntax GetMigrationMethod(int toVersion, string dataArgumentTypeName, CancellationToken ct)
        {
            return SyntaxFactory.MethodDeclaration
                ( attributeLists: default
                , modifiers: SyntaxFactory.TokenList( SyntaxFactory.Token( SyntaxKind.PrivateKeyword )
                                                    , SyntaxFactory.Token( SyntaxKind.StaticKeyword ) )
                , returnType: SyntaxFactory.ParseTypeName( "JToken" )
                , explicitInterfaceSpecifier: default
                , identifier: SyntaxFactory.Identifier( "Migrate_" + toVersion )
                , typeParameterList: default
                , parameterList: SyntaxFactory.ParameterList
                      ( SyntaxFactory.SeparatedList
                            ( new[]
                              {   SyntaxFactory.Parameter( SyntaxFactory.Identifier( "data" ) )
                                               .WithType( SyntaxFactory.ParseTypeName( $"{dataArgumentTypeName}" ) )
                                , SyntaxFactory.Parameter( SyntaxFactory.Identifier( "serializer" ) )
                                               .WithType( SyntaxFactory.ParseTypeName( "JsonSerializer" ) )
                              } ) )
                , constraintClauses: default
                , body: SyntaxFactory.Block( SyntaxFactory.ThrowStatement( SyntaxFactory.ParseExpression( "new System.NotImplementedException()" ) )
                                           , SyntaxFactory.ReturnStatement( SyntaxFactory.ParseExpression( "data" ) ) )
                , expressionBody: default
                , semicolonToken: default)
               .WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation);
        }
    }
}