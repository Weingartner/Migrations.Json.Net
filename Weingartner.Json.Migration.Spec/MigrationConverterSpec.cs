using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Weingartner.Json.Migration.Spec
{
    public class MigrationConverterSpec
    {
        [Fact]
        public void ShouldMigrateSimpleType()
        {
            var settings = GetSerializerSettings();
            var obj = new Address("Street 1");

            var result = RoundTrip(obj, settings);

            result.Street.Should().Be("Street 1 Migrated");
        }

        [Fact]
        public void ShouldMigrateNestedTypes()
        {
            var settings = GetSerializerSettings();
            var obj = new Person("Person 1", new Address("Street 1"));

            var result = RoundTrip(obj, settings);

            result.Name.Should().Be("Person 1 Migrated");
            result.Address.Street.Should().Be("Street 1 Migrated");
        }

        [Fact]
        public void ShouldMigrateAListOfMigratableObjects()
        {
            var settings = GetSerializerSettings();
            var obj = Enumerable.Range(1, 5).Select(i => new Address("Street " + i)).ToList();

            var result = RoundTrip(obj, settings);

            result[4].Street.Should().Be("Street 5 Migrated");
        }

        [Fact(Skip = "Not a requirement by now")]
        public void ShouldMigrateTypesWithReferenceLoops()
        {
            var settings = GetSerializerSettings();
            var obj = new House("Color 2", new[] { new House("Color 1"), new House("Color 3") });

            var result = RoundTrip(obj, settings);

            result.Color.Should().Be("Color 2 Migrated");
            result.Neighbors.First().Color.Should().Be("Color 1 Migrated");
            result.Neighbors.Last().Color.Should().Be("Color 3 Migrated");
        }

        private static JsonSerializerSettings GetSerializerSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new MigrationConverter(new HashBasedDataMigrator<JToken>(new JsonVersionUpdater())));
            return settings;
        }

        private static T RoundTrip<T>(T obj, JsonSerializerSettings settings)
        {
            var serialized = JsonConvert.SerializeObject(obj, settings);
            return JsonConvert.DeserializeObject<T>(serialized, settings);
        }

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedParameter.Local
        // ReSharper disable UnusedField.Compiler
        // ReSharper disable InconsistentNaming
        [Migratable("")]
        private class House
        {
            private const int _version = 1;

            public string Color { get; private set; }
            public IEnumerable<House> Neighbors { get; private set; }

            [JsonConstructor]
            public House(string color, IEnumerable<House> neighbors)
            {
                Color = color;
                Neighbors = neighbors;
            }

            public House(string color)
                : this(color, Enumerable.Empty<House>())
            { }

            private static JToken Migrate_1(JToken data) { return data; }
        }

        [Migratable("")]
        private class Person
        {
            private const int _version = 1;

            public string Name { get; private set; }
            public Address Address { get; private set; }

            public Person(string name, Address address)
            {
                Name = name;
                Address = address;
            }

            private static JToken Migrate_1(JToken data)
            {
                data["Name"] += " Migrated";
                return data;
            }
        }

        [Migratable("")]
        private class Address
        {
            private const int _version = 1;

            public string Street { get; private set; }

            public Address(string street)
            {
                Street = street;
            }

            private static JToken Migrate_1(JToken data)
            {
                data["Street"] += " Migrated";
                return data;
            }
        }
        // ReSharper restore InconsistentNaming
        // ReSharper restore UnusedField.Compiler
        // ReSharper restore UnusedParameter.Local
        // ReSharper restore UnusedMember.Local
    }

}
