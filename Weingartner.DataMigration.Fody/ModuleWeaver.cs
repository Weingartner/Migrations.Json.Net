using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using Weingartner.DataMigration.Common;

namespace Weingartner.DataMigration.Fody
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
                .SingleOrDefault(attr => attr.AttributeType.FullName == "Weingartner.DataMigration.MigratableAttribute");
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
                        "where 'X' is a consecutive number starting from 0. " +
                        "To resolve this error, update the hash passed to the `MigratableAttribute` of the type to '{1}'.",
                        type.FullName,
                        newTypeHash));
            }
        }

        private static void AddVersion(TypeDefinition type)
        {
            var field = CreateBackingField(type);
            InitializeBackingField(field);
            var property = CreateProperty(type, field);
            CreatePropertyGetter(type, property, field);
            //CreatePropertySetter(type, property, field);

            if (TypeIsDataContract(type))
            {
                var dataMemberCtor = typeof(DataMemberAttribute).GetConstructor(Type.EmptyTypes);
                var dataMemberAttribute = new CustomAttribute(type.Module.Import(dataMemberCtor));
                property.CustomAttributes.Add(dataMemberAttribute);
            }
        }

        private static FieldDefinition CreateBackingField(TypeDefinition type)
        {
            var field = new FieldDefinition(Globals.VersionBackingFieldName, FieldAttributes.Private | FieldAttributes.Static, type.Module.TypeSystem.Int32);
            type.Fields.Add(field);
            return field;
        }

        private static void InitializeBackingField(FieldDefinition field)
        {
            var type = field.DeclaringType;
            var staticCtor = type.Methods.SingleOrDefault(m => m.IsStatic && m.Name == ".cctor");
            if (staticCtor == null)
            {
                staticCtor = new MethodDefinition(".cctor",
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                    MethodAttributes.RTSpecialName | MethodAttributes.Static, type.Module.TypeSystem.Void);
                type.Methods.Add(staticCtor);
                var il2 = staticCtor.Body.GetILProcessor();
                il2.Emit(OpCodes.Ret);
            }

            var il = staticCtor.Body.GetILProcessor();
            var first = il.Body.Instructions.First();

            var version =
                type.Methods.Select(m => Regex.Match(m.Name, @"(?<=^Migrate_)(\d+)$"))
                    .Where(m => m.Success)
                    .Select(m => int.Parse(m.Value))
                    .Concat(Enumerable.Repeat(-1, 1))
                    .Max() + 1;
            il.InsertBefore(first, il.Create(OpCodes.Ldc_I4, version));
            il.InsertBefore(first, il.Create(OpCodes.Stsfld, field));
            il.Body.OptimizeMacros();
        }

        private static PropertyDefinition CreateProperty(TypeDefinition type, FieldReference field)
        {
            var property = new PropertyDefinition(Globals.VersionPropertyName, PropertyAttributes.None, field.FieldType);
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
            il.Emit(OpCodes.Ldsfld, field);
            il.Emit(OpCodes.Ret);
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
