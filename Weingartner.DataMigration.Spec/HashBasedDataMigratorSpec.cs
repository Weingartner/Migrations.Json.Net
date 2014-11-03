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
        [InlineData(0, "Name_0_1_2")]
        [InlineData(1, "_1_2")]
        [InlineData(2, "_2")]
        public void ShouldApplyChangesMadeByTheMigrationMethods(int configVersion, string expectedName)
        {
            var configData = CreateConfigurationData(configVersion);

            var sut = CreateMigrator();
            sut.Migrate(ref configData, typeof(FixtureData));

            configData["Name"].Value<string>().Should().Be(expectedName);
        }

        [Fact]
        public void ShouldHaveCorrectVersionAfterMigration()
        {
            var configData = CreateConfigurationData(0);

            var sut = CreateMigrator();
            sut.Migrate(ref configData, typeof(FixtureData));

            configData[Globals.VersionPropertyName].Value<int>().Should().Be(3);
        }

        [Fact]
        public void ShouldWorkWithNonVersionedData()
        {
            var configData = CreateConfigurationData(0);
            ((JObject)configData).Remove(Globals.VersionPropertyName);

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, typeof(FixtureData)))
                .ShouldNotThrow();
        }

        [Fact]
        public void ShouldThrowIfConfigurationDataIsNull()
        {
            JToken configData = null;

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, typeof(FixtureData)))
                .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowIfConfigurationDataTypeIsNull()
        {
            var configData = CreateConfigurationData(0);

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, null))
                .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowIfMigrationMethodHasTooManyParameters()
        {
            var configData = JToken.FromObject(new InvalidData());
            configData[Globals.VersionPropertyName] = 0;

            var sut = CreateMigrator();
            new Action(() => sut.Migrate(ref configData, typeof(InvalidData)))
                .ShouldThrow<MigrationException>();
        }

        private static IMigrateData<JToken> CreateMigrator()
        {
            return new HashBasedDataMigrator<JToken>(new JsonVersionUpdater());
        }

        private static JToken CreateConfigurationData(int version)
        {
            var data = JToken.FromObject(new FixtureData());
            data[Globals.VersionPropertyName] = version;
            return data;
        }

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedParameter.Local
        // ReSharper disable UnusedField.Compiler

        [Migratable("")]
        private class FixtureData
        {
            private static int _version = 3;

            [DataMember]
            public string Name { get; private set; }

            private static void Migrate_0(ref JObject data)
            {
                data["Name"] = "Name_0";
            }

            private static void Migrate_1(ref JObject data)
            {
                data["Name"] += "_1";
            }

            private static void Migrate_2(ref JObject data)
            {
                data["Name"] += "_2";
            }
        }

        [Migratable("")]
        private class InvalidData
        {
            private static int _version = 3;

            [DataMember]
            public string Name { get; private set; }

            private static void Migrate_0(ref JObject data, string additionalData) { }
        }

        // ReSharper restore UnusedField.Compiler
        // ReSharper restore UnusedParameter.Local
        // ReSharper restore UnusedMember.Local
    }
}
