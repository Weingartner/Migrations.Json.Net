using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Xunit;

namespace Weingartner.Json.Migration.Fody.Spec
{
    public class TypeHashGeneratorSpec
    {
        [Fact]
        public void ShouldGenerateCorrectHashForSimpleType()
        {
            var sut = CreateSut();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(Address)));

            hash.Should().Be(GetExpectedHashForAddress());
        }

        [Fact]
        public void ShouldGenerateCorrectHashForNestedTypes()
        {
            var sut = CreateSut();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(Person)));

            hash.Should().Be(GetExpectedHashForPerson());
        }

        [Fact]
        public void ShouldGenerateCorrectHashForNestedEnumerableTypes()
        {
            var sut = CreateSut();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(Club)));

            hash.Should().Be(GetExpectedHashForClub());
        }

        [Fact]
        public void ShouldGenerateCorrectHashForNestedTypesWithGenericArguments()
        {
            var sut = CreateSut();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(ClubEntry)));

            hash.Should().Be(GetExpectedHashForClubEntry());
        }

        [Fact]
        public void ShouldWorkWithCircularTypeDefinitions()
        {
            var sut = CreateSut();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(LinkedPersonEntry)));

            hash.Should().Be(GetExpectedHashForLinkedPersonEntry());
        }

        [Fact]
        public void ShouldGenerateSameHashWhenPropertyPositionsSwitched()
        {
            var sut = CreateSut();
            var hash1 = sut.GenerateHashBase(GetTypeDefinition(typeof(Address)));
            var hash2 = sut.GenerateHashBase(GetTypeDefinition(typeof(Address2))).Replace("/Address2", "/Address");

            hash1.Should().Be(hash2);
        }

        [Fact]
        public void ShouldIgnoreVersionProperty()
        {
            var sut = CreateSut();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(VersionedData)));

            hash.Should().Be(GetExpectedHashForVersionedData());
        }

        [Fact]
        public void ShouldIgnorePropertiesWithoutDataMemberAttributeWhenDeclaringTypeHasDataContractAttribute()
        {
            var sut = CreateSut();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(DataContractWithExcludedProperties)));

            hash.Should().Be(GetExpectedHashForDataContractWithExcludedProperties());
        }

        [Fact]
        public void ShouldNotIgnorePropertiesWithoutDataMemberAttributeWhenDeclaringTypeDoesntHaveDataContractAttribute()
        {
            var sut = CreateSut();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(NonDataContract)));

            hash.Should().Be(GetExpectedHashForNonDataContract());
        }

        // TODO support type hierarchies

        private static TypeHashGenerator CreateSut()
        {
            return new TypeHashGenerator();
        }

        private static TypeDefinition GetTypeDefinition(Type type)
        {
            var module = AssemblyDefinition
                .ReadAssembly(Assembly.GetExecutingAssembly().Location)
                .Modules
                .Single();
            var typeDef = module.Import(type).Resolve();
            return module
                .GetAllTypes()
                .Single(t => t.IsProbablyEqualTo(typeDef));
        }

        private static readonly string BaseName = typeof (TypeHashGeneratorSpec).FullName;

        private static string GetExpectedHashForAddress()
        {
            return "System.String-City|System.String-Street";
        }

        private static string GetExpectedHashForAddressWithType()
        {
            return string.Format("{0}/Address({1})", BaseName, GetExpectedHashForAddress());
        }

        private static string GetExpectedHashForPerson()
        {
            return "System.String-Name|" + GetExpectedHashForAddressWithType() + "-Address";
        }

        private static string GetExpectedHashForPersonWithType()
        {
            return string.Format("{0}/Person({1})", BaseName, GetExpectedHashForPerson());
        }

        private static string GetExpectedHashForClubEntry()
        {
            return "System.Tuple`2(System.Int32-Item1|" + GetExpectedHashForPersonWithType() + "-Item2)-Member";
        }

        private static string GetExpectedHashForClub()
        {
            return "System.Collections.Generic.IDictionary`2(System.Int32|" + GetExpectedHashForPersonWithType() + ")-Members";
        }

        private static string GetExpectedHashForLinkedPersonEntry()
        {
            return string.Format("{0}/LinkedPersonEntry-Next|{1}-Current", BaseName, GetExpectedHashForPersonWithType());
        }

        private static string GetExpectedHashForVersionedData()
        {
            return string.Empty;
        }

        private static string GetExpectedHashForDataContractWithExcludedProperties()
        {
            return string.Format("System.String-IncludedProperty");
        }

        private static string GetExpectedHashForNonDataContract()
        {
            return string.Format("System.Int32-PropertyA|System.String-PropertyB");
        }

        [DataContract]
        private class LinkedPersonEntry
        {
            [DataMember]
            public Person Current { get; private set; }

            [DataMember]
            public LinkedPersonEntry Next { get; private set; }
        }

        [DataContract]
        private class Club
        {
            [DataMember]
            public IDictionary<int, Person> Members { get; private set; }
        }

        [DataContract]
        private class ClubEntry
        {
            [DataMember]
            public Tuple<int, Person> Member { get; private set; }
        }

        [DataContract]
        private class Employee : Person
        {
            [DataMember]
            public string EmployeeId { get; private set; }
        }

        [DataContract]
        private class Person
        {
            [DataMember]
            public string Name { get; private set; }

            [DataMember]
            public Address Address { get; private set; }
        }

        [DataContract]
        private class Address
        {
            [DataMember]
            public string City { get; private set; }

            [DataMember]
            public string Street { get; private set; }
        }

        [DataContract]
        private class Address2
        {
            [DataMember]
            public string Street { get; private set; }

            [DataMember]
            public string City { get; private set; }
        }

        [DataContract]
        private class VersionedData
        {
            [DataMember]
            public string Version { get; private set; }
        }

        [DataContract]
        private class DataContractWithExcludedProperties
        {
            [DataMember]
            public string IncludedProperty { get; private set; }

            public string ExcludedProperty { get; private set; }
        }

        private class NonDataContract
        {
            [DataMember]
            public int PropertyA { get; private set; }

            public string PropertyB { get; private set; }
        }
    }
}
