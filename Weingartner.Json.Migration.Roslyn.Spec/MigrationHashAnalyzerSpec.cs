using System;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Weingartner.Json.Migration.Roslyn.Spec.Helpers;
using Xunit;

namespace Weingartner.Json.Migration.Roslyn.Spec
{
    public class MigrationHashAnalyzerSpec : CodeFixVerifier
    {
        [Fact]
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

        [Fact]
        public void ShouldCreateDiagnosticIfNoHashIsSpecified()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
class TypeName
{
    [DataMember]
    public int A { get; set; }
}";
            var expected = new DiagnosticResult
            {
                Id = MigrationHashAnalyzer.DiagnosticId,
                Message = string.Format(MigrationHashAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "TypeName", "758832573"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 7)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfNoHashIsSpecified2()
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
            var expected = new DiagnosticResult
            {
                Id = MigrationHashAnalyzer.DiagnosticId,
                Message = string.Format(MigrationHashAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "TypeName", "758832573"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 5, 7)
                    }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldNotCreateDiagnosticIfCorrectHashIsSpecified()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable(""758832573"")]
[DataContract]
class TypeName
{
    [DataMember]
    public int A { get; set; }
}";

            VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfInCorrectHashIsSpecified()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable(""758832573"")]
[DataContract]
class TypeName
{
    [DataMember]
    public int A { get; set; }
    [DataMember]
    public double B { get; set; }
}";

            var expected = new DiagnosticResult
                           {
                               Id = MigrationHashAnalyzer.DiagnosticId,
                               Message = string.Format(MigrationHashAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "TypeName", "687340935"),
                               Severity = DiagnosticSeverity.Error,
                               Locations =
                                   new[] {
                                             new DiagnosticResultLocation("Test0.cs", 5, 7)
                                         }
                           };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldFixIfInCorrectIsSpecified()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable(""758832573"")]
[DataContract]
class TypeName
{
    [DataMember]
    public int A { get; set; }
    [DataMember]
    public double B { get; set; }
}";
            
            var expected = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable(""687340935"")]
[DataContract]
class TypeName
{
    [DataMember]
    public int A { get; set; }
    [DataMember]
    public double B { get; set; }
}";

            VerifyCSharpFix(source, expected);
        }

        [Fact]
        public void ShouldFixRecordIfInCorrectIsSpecified()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable(""758832573"")]
[DataContract]
record TypeName
{
    [DataMember]
    public int A { get; set; }
    [DataMember]
    public double B { get; set; }
}";
            
            var expected = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable(""687340935"")]
[DataContract]
record TypeName
{
    [DataMember]
    public int A { get; set; }
    [DataMember]
    public double B { get; set; }
}";

            VerifyCSharpFix(source, expected);
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