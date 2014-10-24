using System;
using System.Runtime.Serialization;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Weingartner.DataMigration.Common;
using Xunit;
using Xunit.Extensions;

namespace Weingartner.DataMigration.Spec
{
    public class HashBasedDataMigratorSpec
    {
        [Theory]
        [InlineData(null, "Name_0_1_2")]
        [InlineData("1", "_1_2")]
        [InlineData("2", "_2")]
        public void ShouldApplyChangesMadeByTheMigrationMethods(string configVersion, string expectedName)
        {
            var configData = CreateConfigurationData(configVersion);

            var sut = CreateMigrator();
            sut.Migrate(ref configData, typeof(FixtureData));

            configData["Name"].Value<string>().Should().Be(expectedName);
        }

        [Fact]
        public void ShouldHaveCorrectVersionAfterMigration()
        {
            var configData = CreateConfigurationData("");

            var sut = CreateMigrator();
            sut.Migrate(ref configData, typeof(FixtureData));

            configData["Version"].Value<string>().Should().Be("3");
        }

        [Fact]
        public void ShouldWorkWithNonVersionedData()
        {
            var configData = CreateConfigurationData("");
            configData.Remove("Version");

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, typeof(FixtureData)))
                .ShouldNotThrow();
        }

        [Fact]
        public void ShouldThrowIfMigrationIsMissing()
        {
            var configData = CreateConfigurationData("InvalidVersion");

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, typeof(FixtureData)))
                .ShouldThrow<MigrationException>();
        }

        [Fact]
        public void ShouldThrowIfConfigurationDataIsNull()
        {
            JObject configData = null;

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, typeof(FixtureData)))
                .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowIfConfigurationDataTypeIsNull()
        {
            var configData = CreateConfigurationData("");

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, null))
                .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowIfMigrationMethodHasTooManyParameters()
        {
            var configData = JObject.FromObject(new InvalidData());
            configData["Version"] = string.Empty;

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, typeof(InvalidData)))
                .ShouldThrow<MigrationException>();
        }

        [Fact]
        public void ShouldThrowIfMultipleMigrationMethodsCanMigrateFromSameVersion()
        {
            var configData = JObject.FromObject(new InvalidData2());
            configData["Version"] = string.Empty;

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, typeof(InvalidData2)))
                .ShouldThrow<MigrationException>();
        }

        [Fact]
        public void ShouldThrowIfCircularMigrationDetected()
        {
            var configData = JObject.FromObject(new InvalidData3());
            configData["Version"] = string.Empty;

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, typeof (InvalidData3)))
                .ShouldThrow<MigrationException>();
        }

        private static IMigrateData<JObject, Type> CreateMigrator()
        {
            return new HashBasedDataMigrator<JObject>(new JObjectHashExtractor());
        }

        private static JObject CreateConfigurationData(string version)
        {
            var data = JObject.FromObject(new FixtureData());
            data["Version"] = version;
            return data;
        }

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedParameter.Local
        // ReSharper disable UnusedField.Compiler
        private class FixtureData
        {
            private static string VersionStatic = "3";

            [DataMember]
            public string Name { get; private set; }

            [Migration("", "1")]
            private static void Migrate_0(ref JObject data)
            {
                data["Name"] = "Name_0";
                data["Version"] = "1";
            }

            [Migration("1", "2")]
            private static void Migrate_1(ref JObject data)
            {
                data["Name"] += "_1";
                data["Version"] = "2";
            }

            [Migration("2", "3")]
            private static void Migrate_2(ref JObject data)
            {
                data["Name"] += "_2";
                data["Version"] = "3";
            }
        }

        private class InvalidData
        {
            private static string VersionStatic = "3";

            [DataMember]
            public string Name { get; private set; }

            [Migration("", "1")]
            private static void Migrate_0(ref JObject data, string additionalData) { }
        }

        private class InvalidData2
        {
            private static string VersionStatic = "3";

            [DataMember]
            public string Name { get; private set; }

            [Migration("", "1")]
            private static void Migrate_0(ref JObject data) { }

            [Migration("", "1")]
            private static void Migrate_1(ref JObject data) { }
        }

        private class InvalidData3
        {
            private static string VersionStatic = "3";

            [DataMember]
            public string Name { get; private set; }

            [Migration("", "a")]
            private static void Migrate_0(ref JObject data) { }

            [Migration("a", "")]
            private static void Migrate_1(ref JObject data) { }
        }
        // ReSharper restore UnusedField.Compiler
        // ReSharper restore UnusedParameter.Local
        // ReSharper restore UnusedMember.Local
    }
}
