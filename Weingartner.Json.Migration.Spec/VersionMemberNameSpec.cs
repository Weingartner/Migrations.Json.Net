using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weingartner.Json.Migration.Common;
using Xunit;
// ReSharper disable All

namespace Weingartner.Json.Migration.Spec
{
    public class VersionMemberNameSpec
    {
        [Fact]
        public void CurrentVersionShouldBeCorrect()
        {
            VersionMemberName.GetCurrentVersion(typeof (TypeWithNoMigrationMethod)).Should().Be(0);
            VersionMemberName.GetCurrentVersion(typeof (TypeWithOneMigrationMethod)).Should().Be(1);
            VersionMemberName.GetCurrentVersion(typeof (TypeWithTwoMigrationMethods)).Should().Be(2);
        }

        [Fact]
        public void VerifyMigrationMethodShouldWork()
        {
            var method = typeof (TypeWithMigrationMethod0).GetMethod("Migrate_0", BindingFlags.NonPublic | BindingFlags.Static);
            var verificationResult = new MigrationMethodVerifier(VersionMemberName.CanAssign)
                .VerifyMigrationMethodSignature(VersionMemberName.GetMigrationMethod(method), null);
            verificationResult.Should().Be(VerificationResultEnum.Ok);
        }
    }

    // ReSharper disable UnusedMember.Local
    public class TypeWithMigrationMethod0
    {
        private static JToken Migrate_0(JToken data, JsonSerializer serializer)
        {
            return data;
        }
    }

    public class TypeWithMigrationMethod2
    {
        private static JToken Migrate_2(JToken data, JsonSerializer serializer)
        {
            return data;
        }
    }

    public class TypeWithUnconsecutiveMigrationMethods
    {
        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            return data;
        }

        private static JToken Migrate_3(JToken data, JsonSerializer serializer)
        {
            return data;
        }
    }

    internal class TypeWithTwoMigrationMethods
    {
        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            return data;
        }

        private static JToken Migrate_2(JToken data, JsonSerializer serializer)
        {
            return data;
        }
    }

    internal class TypeWithOneMigrationMethod
    {
        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            return data;
        }
    }

    internal class TypeWithNoMigrationMethod
    {
    }
    // ReSharper restore UnusedMember.Local
}
