using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using TestHelper;

namespace Weingartner.Json.Migration.Roslyn.Test
{
    [TestClass]
    public class AnalyserSpec : CodeFixVerifier
    {
        [TestMethod]
        public void ShouldNotCreateDiagnosticIfTypeIsNotMigratable()
        {
            var source = @"
using System.Runtime.Serialization;

[DataContract]
class TypeName
{   
}";
            VerifyCSharpDiagnostic(source);
        }

        [TestMethod]
        public void ShouldCreateDiagnosticIfNoHashIsSpecified()
        {
            var source = @"
using Weingartner.Json.Migration;

[Migratable("""")]
class TypeName
{
}";
            var expected = new DiagnosticResult
            {
                Id = MigrationHashAnalyzer.DiagnosticId,
                Message = string.Format(MigrationHashAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "TypeName", "23"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 4, 7)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new MigrationHashAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MigrationHashAnalyzer();
        }
    }
}