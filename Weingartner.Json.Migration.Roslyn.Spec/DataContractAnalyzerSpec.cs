using System;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Weingartner.Json.Migration.Roslyn.Spec.Helpers;
using Xunit;

namespace Weingartner.Json.Migration.Roslyn.Spec
{
    public class DataContractAnalyzerSpec : CodeFixVerifier
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

        [Fact]
        public void ShouldNotCreateDiagnosticIfMigratableTypeHasDataMemberField()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
class TypeName
{
    [DataMember] private readonly int A;

    public TypeName(int a) { A = a; }
}";
            VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public void ShouldNotCreateDiagnosticIfMigratableTypeHasDataMemberProperty()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
class TypeName
{
    [DataMember] public int A { get; set; }
}";
            VerifyCSharpDiagnostic(source);
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