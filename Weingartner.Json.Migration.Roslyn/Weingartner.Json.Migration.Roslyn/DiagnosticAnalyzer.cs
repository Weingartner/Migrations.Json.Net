using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
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
    [Migratable("XXX")]
    class Foo
    {
        [DataMember()]
        public string A { get; set; }

        public WeingartnerJsonMigrationRoslynAnalyzer C { get; set; }
        public string B { get; set; }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class WeingartnerJsonMigrationRoslynAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WeingartnerJsonMigrationRoslyn";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        public static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "DataMigration";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var targetTree = context.SemanticModel.SyntaxTree;
            var root = targetTree.GetRoot();

            var classSyntaxNodes = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(node => MigratableAttributes(node).Any())
                .ToList();

            foreach (var klassSyntaxNode in classSyntaxNodes)
            {
                var migrationHashFromAttribute = GetMigrationHashFromAttribute(klassSyntaxNode);
                var migrationHashCalculated = GetMigrationHashFromType(klassSyntaxNode);
                if (migrationHashCalculated != migrationHashFromAttribute)
                {
                    // Add Diagnostic
                    var diagnostic = Diagnostic.Create(Rule, klassSyntaxNode.GetLocation(), klassSyntaxNode.Identifier.ToString(), migrationHashCalculated, migrationHashFromAttribute);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        public static string GetMigrationHashFromType(BaseTypeDeclarationSyntax klassSyntaxNode)
        {
            var properties = GetDataMemberProperties(klassSyntaxNode);
            return string.Join(";", properties.Select(p => p.Type.ToString() + "|" + p.Identifier.ToString())).GetHashCode().ToString();
        }

        public static string GetMigrationHashFromAttribute(BaseTypeDeclarationSyntax type)
        {
            var argument = MigratableAttributes(type)
                .First()
                .ArgumentList
                ?.Arguments
                .FirstOrDefault();

            return ((argument?.Expression as LiteralExpressionSyntax)?.Token.ValueText) ??  "";
        }

        private static IImmutableList<PropertyDeclarationSyntax> GetDataMemberProperties(BaseTypeDeclarationSyntax type)
        {
            return type.ChildNodes().OfType<PropertyDeclarationSyntax>()
                .Where(p =>
                    p.AttributeLists.SelectMany(l => l.Attributes).Any(a => (a.Name.ToString() == "DataMember")) // TODO
                )
                .ToImmutableList();
        }

        public static IEnumerable<AttributeSyntax> MigratableAttributes(BaseTypeDeclarationSyntax type)
        {
            return type.AttributeLists
                .SelectMany(list => list.Attributes)
                .Where(attr => attr.Name.ToString() == "Migratable"); // TODO
        }
    }
}
