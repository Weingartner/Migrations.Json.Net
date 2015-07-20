using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weingartner.Json.Migration.Common;
using Xunit;

namespace Weingartner.Json.Migration.Spec
{
    public class HashBasedDataMigratorSpec
    {
        public HashBasedDataMigratorSpec()
        {
            _Serializer = new JsonSerializer();
        }

        public static JsonSerializer _Serializer = new JsonSerializer();

        [Theory]
        [InlineData(0, "Name_0_1_2")]
        [InlineData(1, "_1_2")]
        [InlineData(2, "_2")]
        public void ShouldApplyChangesMadeByTheMigrationMethods(int configVersion, string expectedName)
        {
            var configData = CreateConfigurationData(configVersion);

            var sut = CreateMigrator();
            var result = sut.TryMigrate(configData, typeof(FixtureData), _Serializer);

            result["Name"].Value<string>().Should().Be(expectedName);
        }

        [Fact]
        public void ShouldBeAbleToReplaceWholeObject()
        {
            var configData = JToken.FromObject(new[] { 1, 2, 3 });

            var sut = CreateMigrator();
            var result = sut.TryMigrate(configData, typeof(FixtureData2), _Serializer);

            result.Should().BeOfType<JObject>();
        }

        [Fact]
        public void ShouldHaveCorrectVersionAfterMigration()
        {
            var configData = CreateConfigurationData(0);

            var sut = CreateMigrator();
            var result = sut.TryMigrate(configData, typeof(FixtureData), _Serializer);

            result[VersionMemberName.VersionPropertyName].Value<int>().Should().Be(3);
        }

        [Fact]
        public void ShouldWorkWithNonVersionedData()
        {
            var configData = CreateConfigurationData(0);
            ((JObject)configData).Remove(VersionMemberName.VersionPropertyName);

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(configData, typeof(FixtureData), _Serializer)).ShouldNotThrow();
        }

        [Fact]
        public void ShouldThrowIfConfigurationDataIsNull()
        {
            JToken configData = null;

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(configData, typeof(FixtureData), _Serializer)).ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void ShouldThrowIfConfigurationDataTypeIsNull()
        {
            var configData = CreateConfigurationData(0);

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(configData, null, _Serializer)).ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData(typeof(DataWithInvalidMigrationMethod))]
        [InlineData(typeof(DataWithInvalidMigrationMethod2))]
        public void ShouldThrowIfMigrationMethodIsInvalid(Type configType)
        {
            var configData = new JObject();
            configData[VersionMemberName.VersionPropertyName] = 0;

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(configData, configType, _Serializer)).ShouldThrow<MigrationException>();
        }

        [Fact]
        public void ShouldThrowIfVersionFieldIsMissing()
        {
            var configData = JToken.FromObject(new DataWithoutVersion());

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(configData, typeof(DataWithoutVersion), _Serializer)).ShouldThrow<MigrationException>();
        }

        [Fact]
        public void ShouldNotChangeDataWhenTypeIsNotMigratable()
        {
            var configData = JToken.FromObject(new NotMigratableData("Test"));
            var origConfigData = configData.DeepClone();

            var sut = CreateMigrator();
            var result = sut.TryMigrate(configData, typeof(NotMigratableData), _Serializer);
            result.Should().Match((JToken p) => JToken.DeepEquals(p, origConfigData));
        }

        [Fact]
        public void ShouldWorkWithCustomDataConverter()
        {
            var configData = CreateConfigurationFromObject(new FixtureDataWithCustomMigrator(), 0);

            var sut = CreateMigrator();
            var result = sut.TryMigrate(configData, typeof(FixtureDataWithCustomMigrator), _Serializer);

            result["Name"].Value<string>().Should().Be("Name_A_B_C");
        }

        [Fact]
        public void ShouldThrowWhenVersionOfDataIsTooHigh()
        {
            var configData = CreateConfigurationFromObject(new FixtureData(), FixtureData._version + 1);

            var sut = CreateMigrator();
            new Action(() => sut.TryMigrate(configData, typeof(FixtureData), _Serializer)).ShouldThrow<DataVersionTooHighException>();
        }

        [Fact]
        public void ShouldBeAbleToMigrateAnArray()
        {
            var ints = new[] { 3, 4, 5 }.ToList();
            var configData = JToken.FromObject( ints);

            var sut = CreateMigrator();
            var result = sut.TryMigrate(configData, typeof (ArrayFixtureData), _Serializer);
            result["Name"].ToString().Should().Be("Brad");
            result["Data"].ToList().Should().BeEquivalentTo(ints.Select(i=>JToken.FromObject(i)));
        }

        [Migratable("")]
        private class ArrayFixtureData
        {
            public const int _version = 1;

            [DataMember]
            public string Name { get; private set; }

            [DataMember]
            public List<int> Data { get; private set; } 

            private static JObject Migrate_1(JToken data, JsonSerializer serializer)
            {
                var array = data as JArray;

                var obj = new JObject();
                obj["Data"] = array;
                obj["Name"] = "Brad";

                return obj;

            }
        }

        private static IMigrateData<JToken> CreateMigrator()
        {
            return new HashBasedDataMigrator<JToken>(new JsonVersionUpdater());
        }

        private static JToken CreateConfigurationData(int version)
        {
            return CreateConfigurationFromObject(new FixtureData(), version);
        }

        private static JToken CreateConfigurationFromObject(object obj, int version)
        {
            var data = JToken.FromObject(obj);
            data[VersionMemberName.VersionPropertyName] = version;
            return data;
        }

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedParameter.Local
        // ReSharper disable UnusedField.Compiler
        // ReSharper disable InconsistentNaming


        [Migratable("")]
        private class FixtureData
        {
            public const int _version = 3;

            [DataMember]
            public string Name { get; private set; }

            private static JObject Migrate_1(JObject data, JsonSerializer serializer)
            {
                data["Name"] = "Name_0";
                return data;
            }

            private static JObject Migrate_2(JObject data, JsonSerializer serializer)
            {
                data["Name"] += "_1";
                return data;
            }

            private static JObject Migrate_3(JObject data, JsonSerializer serializer)
            {
                data["Name"] += "_2";
                return data;
            }
        }

        [Migratable("")]
        private class FixtureData2
        {
            public const int _version = 1;

            [DataMember]
            public int[] Values { get; private set; }

            private static JToken Migrate_1(JToken data, JsonSerializer serializer)
            {
                data = new JObject { { "Values", data } };
                return data;
            }
        }

        [Migratable("")]
        private class DataWithInvalidMigrationMethod
        {
            public const int _version = 3;

            [DataMember]
            public string Name { get; private set; }

            private static JToken Migrate_1(JToken data, string additionalData) { return data; }
        }

        [Migratable("")]
        private class DataWithInvalidMigrationMethod2
        {
            public const int _version = 3;

            [DataMember]
            public string Name { get; private set; }

            private static object Migrate_1(JToken data) { return data; }
        }

        [Migratable("")]
        private class DataWithoutVersion
        {
            [DataMember]
            public string Name { get; private set; }

            private static JToken Migrate_1(JToken data, JsonSerializer serializer)
            {
                return data;
            }
        }

        private class NotMigratableData
        {
            [DataMember]
            public string Name { get; private set; }

            public NotMigratableData(string name)
            {
                Name = name;
            }

            private static JToken Migrate_1(JToken data, JsonSerializer serializer)
            {
                data["Name"] += " - migrated";
                return data;
            }
        }

        [Migratable("", typeof(FixtureDataMigrator))]
        public class FixtureDataWithCustomMigrator
        {
            public const int _version = 3;

            [DataMember]
            public string Name { get; private set; }
        }

        public class FixtureDataMigrator
        {
            private static JObject Migrate_1(JObject data, JsonSerializer serializer)
            {
                data["Name"] = "Name_A";
                return data;
            }

            private static JObject Migrate_2(JObject data, JsonSerializer serializer)
            {
                data["Name"] += "_B";
                return data;
            }

            private static JObject Migrate_3(JObject data, JsonSerializer serializer)
            {
                data["Name"] += "_C";
                return data;
            }
        }

        // ReSharper restore InconsistentNaming
        // ReSharper restore UnusedField.Compiler
        // ReSharper restore UnusedParameter.Local
        // ReSharper restore UnusedMember.Local
    }
}
