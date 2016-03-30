using System;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Weingartner.Json.Migration.Common;
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
    private JToken Migrate_2(JToken data, JsonSerializer serializer) { return data; }
}";
            
            var expected = new DiagnosticResult
            {
                Id = MigrationMethodAnalyzer.DiagnosticId,
                Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_2", "TypeName", VerificationResultEnum.DoesntStartWithOne),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 20) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfMigrationMethodsAreNotConsecutive()
        {
            var source = @"
using Weingartner.Json.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Migratable("""")]
class TypeName
{
    private JToken Migrate_1(JToken data, JsonSerializer serializer) { }
    private JToken Migrate_3(JToken data, JsonSerializer serializer) { }
    private JToken Migrate_4(JToken data, JsonSerializer serializer) { }
    private JToken Migrate_6(JToken data, JsonSerializer serializer) { }
}";

            var expected = new[] {
                new DiagnosticResult
                {
                    Id = MigrationMethodAnalyzer.DiagnosticId,
                    Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_3", "TypeName", VerificationResultEnum.IsNotConsecutive),
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 20) }
                },
                new DiagnosticResult
                {
                    Id = MigrationMethodAnalyzer.DiagnosticId,
                    Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_6", "TypeName", VerificationResultEnum.IsNotConsecutive),
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 20) }
                }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfDataArgumentTypeIsNotJToken()
        {
            var source = @"
using Weingartner.Json.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Migratable("""")]
class TypeName
{
    private JToken Migrate_1(string data, JsonSerializer serializer) { }
}";

            var expected = new DiagnosticResult
            {
                Id = MigrationMethodAnalyzer.DiagnosticId,
                Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_1", "TypeName", VerificationResultEnum.FirstArgumentMustBeAssignableToJToken),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 20) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfMigrationMethodDoesntHaveTwoParameters()
        {
            var source = @"
using Weingartner.Json.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Migratable("""")]
class TypeName
{
    private JToken Migrate_1(JToken data) { }
    private JToken Migrate_2(JToken data, JsonSerializer serializer, object additionalData) { }
}";

            var expected = new[] {
                new DiagnosticResult
                {
                    Id = MigrationMethodAnalyzer.DiagnosticId,
                    Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_1", "TypeName", VerificationResultEnum.ParameterCountDoesntMatch),
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 20) }
                },
                new DiagnosticResult
                {
                    Id = MigrationMethodAnalyzer.DiagnosticId,
                    Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_2", "TypeName", VerificationResultEnum.ParameterCountDoesntMatch),
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 20) }
                }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfDataArgumentTypeIsNotAssignableFromPreviousReturnType()
        {
            var source = @"
using Weingartner.Json.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Migratable("""")]
class TypeName
{
    private JObject Migrate_1(JToken data, JsonSerializer serializer) { }
    private JToken Migrate_2(JArray data, JsonSerializer serializer) { }
    private JToken Migrate_3(JObject data, JsonSerializer serializer) { }
}";

            var expected = new[] {
                new DiagnosticResult
                {
                    Id = MigrationMethodAnalyzer.DiagnosticId,
                    Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_2", "TypeName", VerificationResultEnum.FirstArgumentMustBeAssignableToReturnTypeOfPreviousMigrationMethod),
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 20) }
                },
                new DiagnosticResult
                {
                    Id = MigrationMethodAnalyzer.DiagnosticId,
                    Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_3", "TypeName", VerificationResultEnum.FirstArgumentMustBeAssignableToReturnTypeOfPreviousMigrationMethod),
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 20) }
                }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfSecondArgumentIsNotAssignableToJsonSerializer()
        {
            var source = @"
using Weingartner.Json.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Migratable("""")]
class TypeName
{
    private JToken Migrate_1(JToken data, string serializer) { }
}";

            var expected = new DiagnosticResult
            {
                Id = MigrationMethodAnalyzer.DiagnosticId,
                Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_1", "TypeName", VerificationResultEnum.SecondArgumentMustBeAssignableToJsonSerializer),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 20) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldCreateDiagnosticIfReturnTypeIsNotAssignableToJToken()
        {
            var source = @"
using Weingartner.Json.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Migratable("""")]
class TypeName
{
    private string Migrate_1(JToken data, JsonSerializer serializer) { }
}";

            var expected = new DiagnosticResult
            {
                Id = MigrationMethodAnalyzer.DiagnosticId,
                Message = string.Format(MigrationMethodAnalyzer.MessageFormat.ToString(CultureInfo.InvariantCulture), "Migrate_1", "TypeName", VerificationResultEnum.ReturnTypeMustBeAssignableToJToken),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 20) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ShouldNotCreateDiagnosticIfMigrationMethodsAreValid()
        {
            var source = @"
using Weingartner.Json.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Migratable("""")]
class TypeName
{
    private JArray Migrate_1(JObject data, JsonSerializer serializer) { }
    private JObject Migrate_2(JArray data, JsonSerializer serializer) { }
    private JToken Migrate_3(JObject data, JsonSerializer serializer) { }
    private JToken Migrate_4(JToken data, JsonSerializer serializer) { }
    private JObject Migrate_5(JToken data, JsonSerializer serializer) { }
    private JObject Migrate_6(JObject data, JsonSerializer serializer) { }
    private JToken Migrate_7(JToken data, JsonSerializer serializer) { }
}";

            VerifyCSharpDiagnostic(source);
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