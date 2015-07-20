using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weingartner.Json.Migration.Common;
using Xunit;

namespace Weingartner.Json.Migration.Spec
{
    public class VerifyingJsonDataMigratorSpec
    {
        public VerifyingJsonDataMigratorSpec()
        {
            _Serializer = new JsonSerializer();
        }

        private JsonSerializer _Serializer;

        [Theory]
        [InlineData("{'Street': 'Teststreet', 'Number': 5, 'Prop': 'Whatever'}", typeof(Address))]
        [InlineData("[{'Street': 'Teststreet', 'Number': 5, 'Prop': 'Whatever'}]", typeof(IEnumerable<Address>))]
        public void ShouldFailVerificationWhenPropertyWasntRemoved(string jsonData, Type type)
        {
            var obj = JToken.Parse(jsonData);
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, type, _Serializer)).ShouldThrow<MigrationException>();
        }

        [Theory]
        [InlineData("{'Street': 'Teststreet'}", typeof(Address))]
        [InlineData("[{'Street': 'Teststreet'}]", typeof(IEnumerable<Address>))]
        public void ShouldFailVerificationWhenDataPropertyIsMissing(string jsonData, Type type)
        {
            var obj = JToken.Parse(jsonData);
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, type, _Serializer)).ShouldThrow<MigrationException>();
        }

        [Theory]
        [InlineData("{'Street': 'Teststreet', 'Number': 5}", typeof(Address))]
        [InlineData("[{'Street': 'Teststreet', 'Number': 5}]", typeof(IEnumerable<Address>))]
        [InlineData("{'Data': 'Test'}", typeof(NoSetter))]
        public void ShouldSucceedVerificationWhenDataIsOk(string jsonData, Type type)
        {
            var obj = JToken.Parse(jsonData);
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, type, _Serializer)).ShouldNotThrow<MigrationException>();
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

            new Action(() => sut.TryMigrate(obj, type, _Serializer)).ShouldNotThrow<MigrationException>();
        }

        [Fact]
        public void ShouldIgnoreVersionFieldInSerializedData()
        {
            var obj = JObject.FromObject(new Address());
            foreach (var fieldName in VersionMemberName.SupportedVersionPropertyNames)
            {
                obj.Add(fieldName, 5);
            }
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, typeof(Address), _Serializer)).ShouldNotThrow<MigrationException>();
        }

        [Fact]
        public void ShouldWorkWithNullData()
        {
            var obj = JValue.CreateNull();
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(obj, typeof(Address), _Serializer)).ShouldNotThrow<Exception>();
        }

        private static IMigrateData<JToken> GetVerifyingMigrator()
        {
            return new VerifyingJsonDataMigrator(new NullMigrator());
        }

        // ReSharper disable UnusedMember.Local
        // ReSharper disable MemberCanBePrivate.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        private class Address
        {
            public string Street { get; set; }
            public int Number { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Local
        // ReSharper restore MemberCanBePrivate.Local

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
                // ReSharper disable once ValueParameterNotUsed
                set { }
            }
        }

        private class NoSetter
        {
            public string Data
            {
                get { return ""; }
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
            public JToken TryMigrate(JToken data, Type dataType, JsonSerializer serializer)
            {
                return data;
            }
        }
    }
}
