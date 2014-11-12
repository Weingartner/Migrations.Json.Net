using System;
using System.Runtime.Serialization;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Weingartner.Json.Migration.Common;
using Xunit;
using Xunit.Extensions;

namespace Weingartner.Json.Migration.Spec
{
    public class HashBasedDataMigratorSpec
    {
        public HashBasedDataMigratorSpec()
        {
            VersionMemberName.TrySetInstance(new ValidCsVersionMemberName());
        }

        [Theory]
        [InlineData(0, "Name_0_1_2")]
        [InlineData(1, "_1_2")]
        [InlineData(2, "_2")]
        public void ShouldApplyChangesMadeByTheMigrationMethods(int configVersion, string expectedName)
        {
            var configData = CreateConfigurationData(configVersion);

            var sut = CreateMigrator();
            sut.TryMigrate(ref configData, typeof(FixtureData));

            configData["Name"].Value<string>().Should().Be(expectedName);
        }

        [Fact]
        public void ShouldBeAbleToReplaceWholeObject()
        {
            var configData = JToken.FromObject(new[] { 1, 2, 3 });

            var sut = CreateMigrator();
            sut.TryMigrate(ref configData, typeof(FixtureData2));

            configData.Should().BeOfType<JObject>();
        }

        [Fact]
        public void ShouldHaveCorrectVersionAfterMigration()
        {
            var configData = CreateConfigurationData(0);

            var sut = CreateMigrator();
            sut.TryMigrate(ref configData, typeof(FixtureData));

            configData[VersionMemberName.Instance.VersionPropertyName].Value<int>().Should().Be(3);
        }

        [Fact]
        public void ShouldWorkWithNonVersionedData()
        {
            var configData = CreateConfigurationData(0);
            ((JObject)configData).Remove(VersionMemberName.Instance.VersionPropertyName);

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(ref configData, typeof(FixtureData)))
                .ShouldNotThrow();
        }

        [Fact]
        public void ShouldThrowIfConfigurationDataIsNull()
        {
            JToken configData = null;

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(ref configData, typeof(FixtureData)))
                .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowIfConfigurationDataTypeIsNull()
        {
            var configData = CreateConfigurationData(0);

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(ref configData, null))
                .ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowIfMigrationMethodHasTooManyParameters()
        {
            var configData = JToken.FromObject(new DataWithInvalidMigrationMethod());
            configData[VersionMemberName.Instance.VersionPropertyName] = 0;

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(ref configData, typeof(DataWithInvalidMigrationMethod)))
                .ShouldThrow<MigrationException>();
        }

        [Fact]
        public void ShouldThrowIfVersionFieldIsMissing()
        {
            var configData = JToken.FromObject(new DataWithoutVersion());

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(ref configData, typeof(DataWithoutVersion)))
                .ShouldThrow<MigrationException>();
        }

        [Fact]
        public void ShouldNotChangeDataWhenTypeIsNotMigratable()
        {
            var configData = JToken.FromObject(new NotMigratableData("Test"));
            var origConfigData = configData.DeepClone();

            var sut = CreateMigrator();
            sut.TryMigrate(ref configData, typeof(NotMigratableData));
            configData.Should().Match((JToken p) => JToken.DeepEquals(p, origConfigData));
        }

        private static IMigrateData<JToken> CreateMigrator()
        {
            return new HashBasedDataMigrator<JToken>(new JsonVersionUpdater());
        }

        private static JToken CreateConfigurationData(int version)
        {
            var data = JToken.FromObject(new FixtureData());
            data[VersionMemberName.Instance.VersionPropertyName] = version;
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
        private class FixtureData2
        {
            private static int _version = 1;

            [DataMember]
            public int[] Values { get; private set; }

            private static void Migrate_0(ref JToken data)
            {
                data = new JObject { { "Values", data } };
            }
        }

        [Migratable("")]
        private class DataWithInvalidMigrationMethod
        {
            private static int _version = 3;

            [DataMember]
            public string Name { get; private set; }

            private static void Migrate_0(ref JToken data, string additionalData) { }
        }

        [Migratable("")]
        private class DataWithoutVersion
        {
            [DataMember]
            public string Name { get; private set; }

            private static void Migrate_0(ref JToken data) { }
        }

        private class NotMigratableData
        {
            [DataMember]
            public string Name { get; private set; }

            public NotMigratableData(string name)
            {
                Name = name;
            }

            private static void Migrate_0(ref JToken data)
            {
                data["Name"] += " - migrated";
            }
        }

        // ReSharper restore UnusedField.Compiler
        // ReSharper restore UnusedParameter.Local
        // ReSharper restore UnusedMember.Local
    }
}
