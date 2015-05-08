using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

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

            new Action(() => sut.TryMigrate(ref obj, type)).ShouldThrow<MigrationException>();
        }

        [Theory]
        [InlineData("{'Street': 'Teststreet'}", typeof(Address))]
        [InlineData("[{'Street': 'Teststreet'}]", typeof(IEnumerable<Address>))]
        public void ShouldFailVerificationWhenDataPropertyIsMissing(string jsonData, Type type)
        {
            var obj = JToken.Parse(jsonData);
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(ref obj, type)).ShouldThrow<MigrationException>();
        }

        [Theory]
        [InlineData("{'Street': 'Teststreet', 'Number': 5}", typeof(Address))]
        [InlineData("[{'Street': 'Teststreet', 'Number': 5}]", typeof(IEnumerable<Address>))]
        public void ShouldSucceedVerificationWhenDataIsOk(string jsonData, Type type)
        {
            var obj = JToken.Parse(jsonData);
            var sut = GetVerifyingMigrator();

            new Action(() => sut.TryMigrate(ref obj, type)).ShouldNotThrow<MigrationException>();
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
        // ReSharper restore UnusedMember.Local

        private class NullMigrator : IMigrateData<JToken>
        {
            public void TryMigrate(ref JToken data, Type dataType)
            {
            }
        }
    }
}
