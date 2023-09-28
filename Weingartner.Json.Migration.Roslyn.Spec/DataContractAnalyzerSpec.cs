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

        [Fact]
        public void ShouldNotCreateDiagnosticIfMigratableRecordHasDataMemberProperty()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
record TypeName
{
    [DataMember] public int A { get; set; }
}";

            VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public void ShouldNotCreateDiagnosticIfMigratableRecordHasDataMemberPropertyInPrimaryConstructor()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
record TypeName([property: DataMember]int A)
{
}";

            VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public void ShouldNotCreateDiagnosticIfMigratableRecordHasDataMemberPropertyAndTargetedAttributeInPrimaryConstructor()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
record TypeName([property: DataMember]int A)
{
    [DataMember] public int B { get; set; }
}";

            VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public void ShouldNotCreateDiagnosticIfMigratableRecordHasDataMemberPropertyAndTargetedAttributeInPrimaryConstructorMixedWithNonDataMemberProperty()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
record TypeName([property: DataMember]int A)
{
    [DataMember] public int B { get; set; }
    public int C { get; set; }
}";

            VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfMigratableRecordDoesntHaveDataMemberPropertyInPrimaryConstructor()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
record TypeName(int A)
{
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
            VerifyCSharpDiagnostic(source,expected);
        }

        [Fact]
        public void ShouldNotCreateDiagnosticIfMigratableRecordHasDataMemberField()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
record TypeName
{
    [DataMember] private readonly int A;

    public TypeName(int a) { A = a; }
}";

            VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfMigratableRecordDoesntHaveDataContractAttributeSet()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
record TypeName
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
        public void ShouldCreateDiagnosticIfMigratableRecordDoesntHaveDataMemberAttributeSet()
        {
            var source = @"
using Weingartner.Json.Migration;
using System.Runtime.Serialization;

[Migratable("""")]
[DataContract]
record TypeName
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