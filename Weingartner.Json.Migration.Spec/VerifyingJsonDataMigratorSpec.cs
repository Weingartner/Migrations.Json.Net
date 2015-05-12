using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration.Spec
{
    public class VerifyingJsonDataMigratorSpec
    {
        [Theory]
        [InlineData("{'Street': 'Teststreet', 'Number': 5, 'Prop': 'Whatever'}", typeof(Address))]
        [InlineData("[{'Street': 'Teststreet', 'Number': 5, 'Prop': 'Whatever'}]", typeof(IEnumerable<Address>))]
        public void ShouldFailVerificationWhenPropertyWasntRemoved(string jsonData, Type type)
        {
            var obj = JToken.Parse(jsonData);
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, type)).ShouldThrow<MigrationException>();
        }

        [Theory]
        [InlineData("{'Street': 'Teststreet'}", typeof(Address))]
        [InlineData("[{'Street': 'Teststreet'}]", typeof(IEnumerable<Address>))]
        public void ShouldFailVerificationWhenDataPropertyIsMissing(string jsonData, Type type)
        {
            var obj = JToken.Parse(jsonData);
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, type)).ShouldThrow<MigrationException>();
        }

        [Theory]
        [InlineData("{'Street': 'Teststreet', 'Number': 5}", typeof(Address))]
        [InlineData("[{'Street': 'Teststreet', 'Number': 5}]", typeof(IEnumerable<Address>))]
        public void ShouldSucceedVerificationWhenDataIsOk(string jsonData, Type type)
        {
            var obj = JToken.Parse(jsonData);
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, type)).ShouldNotThrow<MigrationException>();
        }

        [Theory]
        [InlineData(typeof(NoDataMemberAttribute))]
        [InlineData(typeof(NoJsonPropertyAttribute))]
        [InlineData(typeof(WithJsonIgnoreAttribute))]
        [InlineData(typeof(PrivateProperty))]
        [InlineData(typeof(PrivateGetter))]
        [InlineData(typeof(NoGetter))]
        [InlineData(typeof(MigratableType))]
        public void ShouldIgnoreNotSerializedProperties(Type type)
        {
            var obj = new JObject();
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, type)).ShouldNotThrow<MigrationException>();
        }

        [Fact]
        public void ShouldIgnoreSerializedVersionField()
        {
            var obj = new JObject
            {
                { VersionMemberName.VersionPropertyName, 5 },
                { "Street", "Teststreet" },
                { "Number", 5 }
            };
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, typeof(Address))).ShouldNotThrow<MigrationException>();
        }

        private static IMigrateData<JToken> GetVerifyingMigrator()
        {
            return new VerifyingJsonDataMigrator(new NullMigrator());
        }

        // ReSharper disable UnusedMember.Local
        private class Address
        {
            public string Street { get; set; }
            public int Number { get; set; }
        }

        [DataContract]
        private class NoDataMemberAttribute
        {
            public string Data { get; set; }
        }

        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        private class NoJsonPropertyAttribute
        {
            public string Data { get; set; }
        }

        [JsonObject]
        private class WithJsonIgnoreAttribute
        {
            [JsonIgnore]
            public string Data { get; set; }
        }

        private class PrivateProperty
        {
            private string Data { get; set; }
        }

        private class PrivateGetter
        {
            public string Data { private get; set; }
        }

        private class NoGetter
        {
            public string Data
            {
                set { }
            }
        }

        [Migratable("")]
        public class MigratableType
        {
            public int Version
            {
                get { return 0; }
            }
        }

        // ReSharper restore UnusedMember.Local

        private class NullMigrator : IMigrateData<JToken>
        {
            public JToken TryMigrate(JToken data, Type dataType)
            {
                return data;
            }
        }
    }
}
