using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;

namespace Weingartner.DataMigration.Fody
{
    public class ModuleWeaver
    {
        public ModuleDefinition ModuleDefinition { get; set; }

        public void Execute()
        {
            ModuleDefinition
                .Types
                .Where(t => t
                    .CustomAttributes
                    .Select(attr => attr.AttributeType)
                    .Any(attrType => attrType.FullName == "Weingartner.DataMigration.MigratableAttribute")) // TODO implement proper equality method
                .ToList()
                .ForEach(CheckMigrationAndAddVersion);
        }

        private void CheckMigrationAndAddVersion(TypeDefinition type)
        {
            CheckMigration(type);
            AddVersion(type);
        }

        private static void CheckMigration(TypeDefinition type)
        {
            var dummyData = new object();
            new MigrationTestRunner().Migrate(ref dummyData, type);
        }

        private void AddVersion(TypeDefinition type)
        {
            var field = CreateBackingField(type);
            InitializeBackingField(field);
            var property = CreateProperty(type, field);
            CreatePropertyGetter(type, property, field);
            //CreatePropertySetter(type, property, field);

            if (TypeIsDataContract(type))
            {
                var dataMemberCtor = typeof(DataMemberAttribute).GetConstructor(Type.EmptyTypes);
                var dataMemberAttribute = new CustomAttribute(ModuleDefinition.Import(dataMemberCtor));
                property.CustomAttributes.Add(dataMemberAttribute);
            }
        }

        private FieldDefinition CreateBackingField(TypeDefinition type)
        {
            var field = new FieldDefinition("_Version", FieldAttributes.Private | FieldAttributes.Static, ModuleDefinition.TypeSystem.String);
            type.Fields.Add(field);
            return field;
        }

        private void InitializeBackingField(FieldDefinition field)
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

            var hashCode = GenerateHash(type);
            il.InsertBefore(first, il.Create(OpCodes.Ldstr, hashCode));
            il.InsertBefore(first, il.Create(OpCodes.Stsfld, field));
        }

        private static string GenerateHash(TypeDefinition type)
        {
            return new TypeHashGenerator().GenerateHash(type);
        }

        private static PropertyDefinition CreateProperty(TypeDefinition type, FieldReference field)
        {
            const string propertyName = "Version";
            var property = new PropertyDefinition(propertyName, PropertyAttributes.None, field.FieldType);
            type.Properties.Add(property);
            return property;
        }

        private void CreatePropertyGetter(TypeDefinition type, PropertyDefinition property, FieldDefinition field)
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

        private void CreatePropertySetter(TypeDefinition type, PropertyDefinition property, FieldReference field)
        {
            var setter = new MethodDefinition("set_" + property.Name,
                MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                ModuleDefinition.TypeSystem.Void) { IsGetter = true };
            setter.Parameters.Add(new ParameterDefinition(field.FieldType));
            property.SetMethod = setter;
            type.Methods.Add(setter);

            var il = setter.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
        }

        private bool TypeIsDataContract(TypeDefinition type)
        {
            var dataContractAttribute = ModuleDefinition.Import(typeof(DataContractAttribute)).Resolve();
            return type.CustomAttributes
                .Select(attr => attr.AttributeType)
                .Any(attrType => attrType.IsProbablyEqualTo(dataContractAttribute));
        }
    }
}
