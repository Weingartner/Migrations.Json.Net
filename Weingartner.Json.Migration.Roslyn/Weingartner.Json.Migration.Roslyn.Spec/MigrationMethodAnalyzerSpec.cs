using System;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Weingartner.Json.Migration.Roslyn.Spec
{
    public class MigrationMethodAnalyzerSpec : CodeFixVerifier
    {
        [Fact]
        public void ShouldNotCreateDiagnosticIfTypeIsNotMigratable()
        {
            var source = @"
class TypeName
{
    private void Migrate_1() { }
}";
            VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfMigrationMethodsDontStartWith1()
        {
            var source = @"
using Weingartner.Json.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Migratable("""")]
class TypeName
{
    private JToken Migrate_2(JToken data, JsonSerializer serializer) { }
}";
            
            var expected = new DiagnosticResult
            {
                Id = MigrationMethodAnalyzer.DiagnosticId,
                Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_2", "TypeName", "asd"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 7)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            throw new NotImplementedException();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MigrationMethodAnalyzer();
        }
    }
}