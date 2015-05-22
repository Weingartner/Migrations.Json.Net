using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Weingartner.Json.Migration.Common;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using System.Threading;

namespace Weingartner.Json.Migration.Fody
{
    public class ModuleWeaver
    {
        public ModuleDefinition ModuleDefinition { get; set; }

        public void Execute()
        {
            try
            {
                ModuleDefinition
                    .GetTypes()
                    .Where(IsMigratable)
                    .ToList()
                    .ForEach(CheckMigrationAndAddVersion);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured while executing Weingartner.Json.Migration.Fody: {0}", e);
            }
        }

        private static bool IsMigratable(TypeDefinition type)
        {
            return GetMigratableAttribute(type) != null;
        }

        private static CustomAttribute GetMigratableAttribute(ICustomAttributeProvider customAttributeProvider)
        {
            return customAttributeProvider
                .CustomAttributes
                .SingleOrDefault(attr => attr.AttributeType.FullName == "Weingartner.Json.Migration.MigratableAttribute");
        }

        private static void CheckMigrationAndAddVersion(TypeDefinition type)
        {
            CheckMigration(type);
            AddVersion(type);
        }

        private static void CheckMigration(TypeDefinition type)
        {
            CheckHash(type);
            CheckConsecutiveMigrationMethods(type);
        }

        private static void CheckHash(TypeDefinition type)
        {
            var oldTypeHash = (string)GetMigratableAttribute(type)
                .ConstructorArguments.First().Value;

            var newTypeHash = new TypeHashGenerator().GenerateHash(type);

            if (oldTypeHash != newTypeHash)
            {
                var thread = new Thread(() => Clipboard.SetText(newTypeHash));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                throw new MigrationException(
                    string.Format(
                        "Type '{1}' has changed.{0}" +
                        "If you think that a migration is needed, add a migration method with the following signature:{0}" +
                        "private static void Migrate_{2}(ref TODO data){0}{{{0}// TODO Migrate data{0}}}{0}" +
                        "To resolve this error, update the hash passed to the `MigratableAttribute` of the type to '{3}'.{0}" +
                        "The hash should be in your clipboard.",
                        Environment.NewLine,
                        type.FullName,
                        GetVersionNumber(type) + 1,
                        newTypeHash));
            }
        }

        private static void CheckConsecutiveMigrationMethods(TypeDefinition type)
        {
            var migrationMethodNumbers = GetMigrationMethodVersions(type);
            var error = migrationMethodNumbers
                .Select((version, index) => new { index = index + 1, version })
                .FirstOrDefault(x => x.index != x.version);
            if (error != null)
            {
                throw new MigrationException(
                    string.Format(
                        "The migration methods of type '{0}' are erroneous, because there is no migration to version {1}. " +
                        "Migration methods must be named 'Migrate_X', where X is a consecutive number starting from 1. " +
                        "Furthermore, they must be private, static and have one ref parameter.",
                        type.FullName,
                        error.index));
            }
        }

        private static void AddVersion(TypeDefinition type)
        {
            var field = CreateBackingField(type);
            var property = CreateProperty(type, field);
            CreatePropertyGetter(type, property, field);

            if (TypeIsDataContract(type))
            {
                var dataMemberCtor = typeof(DataMemberAttribute).GetConstructor(Type.EmptyTypes);
                var dataMemberAttribute = new CustomAttribute(type.Module.Import(dataMemberCtor));
                property.CustomAttributes.Add(dataMemberAttribute);
            }
        }

        private static FieldDefinition CreateBackingField(TypeDefinition type)
        {
            var field = new FieldDefinition(
                VersionMemberName.VersionBackingFieldName
                , FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal
                , type.Module.TypeSystem.Int32);

            var version = GetVersionNumber(type);
            field.Constant = version;
            field.HasDefault = true;

            type.Fields.Add(field);
            return field;
        }

        private static PropertyDefinition CreateProperty(TypeDefinition type, FieldReference field)
        {
            var property = new PropertyDefinition(VersionMemberName.VersionPropertyName, PropertyAttributes.None, field.FieldType);
            type.Properties.Add(property);
            return property;
        }

        private static void CreatePropertyGetter(TypeDefinition type, PropertyDefinition property, IConstantProvider valueProvider)
        {
            var getter = new MethodDefinition("get_" + property.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                property.PropertyType) { IsGetter = true };
            property.GetMethod = getter;
            type.Methods.Add(getter);

            var il = getter.Body.GetILProcessor();
            il.Emit(OpCodes.Ldc_I4, (int)valueProvider.Constant);
            il.Emit(OpCodes.Ret);

            il.Body.OptimizeMacros();
        }

        private static int GetVersionNumber(TypeDefinition type)
        {
            var version = GetMigrationMethodVersions(type)
                .Concat(Enumerable.Repeat(0, 1))
                .Max();
            return version;
        }

        private static IEnumerable<int> GetMigrationMethodVersions(TypeDefinition type)
        {
            return type.Methods
                .Where(m => m.IsStatic && m.IsPrivate)
                .Where(m => m.Parameters.Count == 1)
                .Select(m => Regex.Match(m.Name, @"(?<=^Migrate_)(\d+)$"))
                .Where(m => m.Success)
                .Select(m => int.Parse(m.Value));
        }

        private static bool TypeIsDataContract(TypeDefinition type)
        {
            var dataContractAttribute = type.Module.Import(typeof(DataContractAttribute)).Resolve();
            return type.CustomAttributes
                .Select(attr => attr.AttributeType)
                .Any(attrType => attrType.IsProbablyEqualTo(dataContractAttribute));
        }
    }
}
