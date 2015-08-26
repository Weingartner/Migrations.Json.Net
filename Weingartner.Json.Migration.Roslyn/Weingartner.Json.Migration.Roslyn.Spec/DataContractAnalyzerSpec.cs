using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Globalization;
using TestHelper;
using Xunit;

namespace Weingartner.Json.Migration.Roslyn.Test
{
    public class DataContractAnalyserSpec : CodeFixVerifier
    {
        [Fact]
        public void ShouldNotCreateDiagnosticIfTypeIsNotMigratable()
        {
            var source = @"
class TypeName
{   
}";
            VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfMigratableTypeDoesntHaveDataContractAttributeSet()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
class TypeName
{
    [DataMember]
    public int A { get; set; }
}";
            var expected = new DiagnosticResult
            {
                Id = DataContractAnalyzer.DiagnosticId,
                Message = string.Format(DataContractAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "TypeName"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 7)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfMigratableTypeDoesntHaveDataMemberProperty()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
class TypeName
{
    public int A { get; set; }
}";
            var expected = new DiagnosticResult
            {
                Id = DataContractAnalyzer.DiagnosticId,
                Message = string.Format(DataContractAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "TypeName"),
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
            return new DataContractAnalyzer();
        }
    }
}