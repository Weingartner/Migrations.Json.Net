using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Weingartner.Json.Migration.Common;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;

namespace Weingartner.Json.Migration.Fody
{
    public class ModuleWeaver
    {
        public ModuleDefinition ModuleDefinition { get; set; }

        public void Execute()
        {
            ModuleDefinition
                .Types
                .Where(IsMigratable)
                .ToList()
                .ForEach(CheckMigrationAndAddVersion);
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
            var oldTypeHash = (string)GetMigratableAttribute(type)
                .ConstructorArguments.Single().Value;

            var newTypeHash = new TypeHashGenerator().GenerateHash(type);

            if (oldTypeHash != newTypeHash)
            {
                throw new MigrationException(
                    string.Format(
                        "Type '{0}' has changed. " +
                        "If you think that a migration is needed, add a private static method named 'Migrate_X', " +
                        "where 'X' is a consecutive number starting from 1. " +
                        "To resolve this error, update the hash passed to the `MigratableAttribute` of the type to '{1}'.",
                        type.FullName,
                        newTypeHash));
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
                VersionMemberName.Instance.VersionBackingFieldName
                , FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Literal
                , type.Module.TypeSystem.Int32);

            var version =
                type.Methods.Select(m => Regex.Match(m.Name, @"(?<=^Migrate_)(\d+)$"))
                    .Where(m => m.Success)
                    .Select(m => int.Parse(m.Value))
                    .Concat(Enumerable.Repeat(0, 1))
                    .Max();
            field.Constant = version;
            field.HasDefault = true;

            type.Fields.Add(field);
            return field;
        }

        private static PropertyDefinition CreateProperty(TypeDefinition type, FieldReference field)
        {
            var property = new PropertyDefinition(VersionMemberName.Instance.VersionPropertyName, PropertyAttributes.None, field.FieldType);
            type.Properties.Add(property);
            return property;
        }

        private static void CreatePropertyGetter(TypeDefinition type, PropertyDefinition property, FieldDefinition field)
        {
            var getter = new MethodDefinition("get_" + property.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                property.PropertyType) { IsGetter = true };
            property.GetMethod = getter;
            type.Methods.Add(getter);

            var il = getter.Body.GetILProcessor();
            il.Emit(OpCodes.Ldc_I4, (int)field.Constant);
            il.Emit(OpCodes.Ret);

            il.Body.OptimizeMacros();
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
