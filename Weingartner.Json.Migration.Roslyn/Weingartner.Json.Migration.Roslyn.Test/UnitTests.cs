using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using TestHelper;
using Weingartner.Json.Migration.Roslyn;

namespace Weingartner.Json.Migration.Roslyn.Test
{
    [TestClass]
    public class AnalyserSpec : CodeFixVerifier
    {
        [TestMethod]
        public void ShouldCreateDiagnosticIfTypeHasNoDataMember()
        {
            var source = @"
using Weingartner.Json.Migration;
[Migratable(""XX"")]
class TypeName
{   
}";

            var expected = new DiagnosticResult
            {
                Id = WeingartnerJsonMigrationRoslynAnalyzer.DiagnosticId,
                Message = string.Format(WeingartnerJsonMigrationRoslynAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "TypeName", "AA", "BB"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 4, 7)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [TestMethod]
        public void ShouldNotCreateDiagnosticIfPropertyWithDataMemberExists()
        {
            var source = @"
using Weingartner.Json.Migration;
[Migratable(""XX"")]
class TypeName
{   
    [System.Runtime.Serialization.DataMember]
    public void Prop1 { get; set; }
}";

            VerifyCSharpDiagnostic(source);
        }


        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new WeingartnerJsonMigrationRoslynCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new WeingartnerJsonMigrationRoslynAnalyzer();
        }
    }
}