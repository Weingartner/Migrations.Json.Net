using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Threading;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Weingartner.Json.Migration.Common;
using Xunit;
using Assembly = System.Reflection.Assembly;
using BindingFlags = System.Reflection.BindingFlags;
using FieldAttributes = System.Reflection.FieldAttributes;
using System.Threading.Tasks;

namespace Weingartner.Json.Migration.Fody.Spec
{
    public class ModuleWeaverSpec : IDisposable
    {
        private readonly ConcurrentBag<string> _CreatedTestFiles = new ConcurrentBag<string>();

        private const string EmptyTypeHash = "da39a3ee5e6b4b0d3255bfef95601890afd80709";

        private readonly ModuleDefinition _JsonNetDll;
        private readonly ModuleDefinition _MigrationDll;

        public ModuleWeaverSpec()
        {
            const string jsonNetDllPath = @"..\..\..\packages\Newtonsoft.Json.6.0.8\lib\net45\Newtonsoft.Json.dll";
            File.Copy(jsonNetDllPath, Path.GetFileName(jsonNetDllPath), true);
            _CreatedTestFiles.Add(Path.GetFileName(jsonNetDllPath));
            _JsonNetDll = ModuleDefinition.ReadModule(Path.GetFileName(jsonNetDllPath));

            var migrationDllPath = string.Format(@"..\..\..\Weingartner.Json.Migration\bin\{0}\Weingartner.Json.Migration.dll", Configuration);
            File.Copy(migrationDllPath, Path.GetFileName(migrationDllPath), true);
            _CreatedTestFiles.Add(Path.GetFileName(migrationDllPath));
            _MigrationDll = ModuleDefinition.ReadModule(Path.GetFileName(migrationDllPath));
        }

        public static string Configuration
        {
            get
            {
#if DEBUG
                return "Debug";
#else
                return "Release";
#endif
            }
        }

        [Fact]
        public void ShouldInjectVersionProperty()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestData");
            var instance = Activator.CreateInstance(type);

            var property = instance.GetType().GetProperty(VersionMemberName.VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            property.Should().NotBeNull();
        }

        [Fact]
        public void ShouldInjectCorrectVersionInTypeThatHasMigrationMethods()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestData");
            var instance = Activator.CreateInstance(type);

            var property = instance.GetType().GetProperty(VersionMemberName.VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            property.GetValue(instance).Should().Be(1);
        }

        [Fact]
        public void ShouldInjectCorrectVersionInTypeThatHasNoMigrationMethods()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestDataWithoutMigration");
            var instance = Activator.CreateInstance(type);

            var property = instance.GetType().GetProperty(VersionMemberName.VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            property.GetValue(instance).Should().Be(0);
        }

        [Fact]
        public void ShouldInjectDataMemberAttributeIfTypeHasDataContractAttribute()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestDataContract");

            type.GetProperty(VersionMemberName.VersionPropertyName)
                .CustomAttributes
                .Select(attr => attr.AttributeType)
                .Should()
                .Contain(t => t == typeof(DataMemberAttribute));
        }

        [Fact]
        public void ShouldHaveVersion0WhenNoMigrationMethodExists()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestDataWithoutMigration");

            // ReSharper disable once PossibleNullReferenceException
            ((int)type.GetField(VersionMemberName.VersionBackingFieldName, BindingFlags.Static | BindingFlags.Public).GetValue(null)).Should().Be(0);
        }

        [Fact]
        public void ShouldCreateConstVersionField()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestData");
            var instance = Activator.CreateInstance(type);

            var field = instance.GetType().GetField(VersionMemberName.VersionBackingFieldName, BindingFlags.Static | BindingFlags.Public);
            const FieldAttributes attributes = (FieldAttributes.Literal | FieldAttributes.Static);
            // ReSharper disable once PossibleNullReferenceException
            (field.Attributes & attributes).Should().Be(attributes);
        }

        [Fact]
        public void ShouldThrowWhenWeavingInvalidAssembly()
        {
            new Action(WeaveTypeWithWrongHash)
                .ShouldThrow<MigrationException>()
                .Where(e => e.Message.Contains("add a migration method"));
        }


        [Fact]
        public void ShouldThrowWhenMigratableTypeHasNonConsecutiveMigrationMethods()
        {
            new Action(WeaveTypeWithNonConsecutiveMigrationMethods)
                .ShouldThrow<MigrationException>()
                .Where(e => e.Message.Contains("there is no migration to version"));
        }

        [Fact]
        public async Task ShouldPutTextToClipboardWhenCalledFromNonStaThread()
        {
            var clipboardText = string.Empty;
            var thread = new Thread(() =>
            {
                try
                {
                    RunInStaThread(Clipboard.Clear);
                    WeaveTypeWithWrongHash();
                }
                catch (MigrationException)
                {
                }
                finally
                {
                    clipboardText = RunAndGetInStaThread(Clipboard.GetText);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            clipboardText.Should().NotBeEmpty();
        }

        [Fact]
        public void ShouldFindNestedTypes()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TopLevelType+NestedType");
            type.GetProperty(VersionMemberName.VersionPropertyName).Should().NotBeNull();
        }

        [Fact]
        public void ShouldWorkWithCustomMigrator()
        {
            Assembly assembly;
            new Action(() => WeaveValidAssembly(out assembly)).ShouldNotThrow<MigrationException>();
        }

        private void WeaveValidAssembly(out Assembly newAssembly)
        {
            string oldAssemblyPath, newAssemblyPath;
            WeaveValidAssembly(out oldAssemblyPath, out newAssemblyPath);

            Verifier.Verify(oldAssemblyPath, newAssemblyPath);

            newAssembly = Assembly.LoadFrom(Path.GetFullPath(newAssemblyPath));
        }

        private void WeaveValidAssembly(out string oldAssemblyPath, out string newAssemblyPath)
        {
            var guid = Guid.NewGuid();

            var module = CreateTestModule();

            var testDataType = AddMigratableType("TestData", EmptyTypeHash, module);
            AddMigrationMethod(testDataType, 1);

            var testDataContractType = AddMigratableType("TestDataContract", EmptyTypeHash, module);
            testDataContractType.CustomAttributes.Add(new CustomAttribute(module.Import(typeof(DataContractAttribute).GetConstructor(Type.EmptyTypes))));
            AddMigrationMethod(testDataContractType, 1);

            AddMigratableType("TestDataWithoutMigration", EmptyTypeHash, module);

            var topLevelType = AddType("TopLevelType", module);
            var nestedType = CreateMigratableType("NestedType", EmptyTypeHash, module);
            nestedType.Namespace = string.Empty;
            nestedType.Attributes &= ~TypeAttributes.Public;
            nestedType.Attributes |= TypeAttributes.NestedPublic;
            topLevelType.NestedTypes.Add(nestedType);

            var customMigratorType = AddType("CustomMigrator", module);
            AddMigrationMethod(customMigratorType, 1);
            AddMigrationMethod(customMigratorType, 2);
            AddMigrationMethod(customMigratorType, 3);
            var typeWithCustomMigrator = CreateMigratableTypeWithCustomMigrator("TypeWithCustomMigrator", EmptyTypeHash, customMigratorType, module);
            module.Types.Add(typeWithCustomMigrator);

            oldAssemblyPath = string.Format("Test.old.{0}.dll", guid);
            newAssemblyPath = string.Format("Test.new.{0}.dll", guid);
            Weave(oldAssemblyPath, newAssemblyPath, module);
            _CreatedTestFiles.Add(oldAssemblyPath);
            _CreatedTestFiles.Add(newAssemblyPath);
        }

        private static void Weave(string oldAssemblyPath, string newAssemblyPath, ModuleDefinition module)
        {
            module.Write(oldAssemblyPath);

            var weavingTask = new ModuleWeaver { ModuleDefinition = module };
            weavingTask.Execute();

            module.Write(newAssemblyPath);
        }

        private void WeaveTypeWithWrongHash()
        {
            var module = CreateTestModule();

            AddMigratableType("TestData", "wronghash", module);

            var weavingTask = new ModuleWeaver { ModuleDefinition = module };
            weavingTask.Execute();
        }

        private void WeaveTypeWithNonConsecutiveMigrationMethods()
        {
            var module = CreateTestModule();

            var type = AddMigratableType("TestData", EmptyTypeHash, module);
            AddMigrationMethod(type, 1);
            AddMigrationMethod(type, 2);
            AddMigrationMethod(type, 4);

            var weavingTask = new ModuleWeaver { ModuleDefinition = module };
            weavingTask.Execute();
        }

        private ModuleDefinition CreateTestModule()
        {
            var module = ModuleDefinition.CreateModule("Test", ModuleKind.Dll);
            module.AssemblyReferences.Add(_JsonNetDll.Assembly.Name);
            module.AssemblyReferences.Add(_MigrationDll.Assembly.Name);
            return module;
        }

        private TypeDefinition AddMigratableType(string name, string typeHash, ModuleDefinition module)
        {
            var type = CreateMigratableType(name, typeHash, module);
            module.Types.Add(type);
            return type;
        }

        private TypeDefinition CreateMigratableType(string name, string typeHash, ModuleDefinition module)
        {
            var type = CreateType(name, module);

            var attributeCtor = GetMigratableAttribute(c => c.Parameters.Count == 1);
            AddMigratableAttribute(type, attributeCtor, typeHash, module);

            return type;
        }

        private TypeDefinition CreateMigratableTypeWithCustomMigrator(string name, string typeHash, TypeReference migratorType, ModuleDefinition module)
        {
            var type = CreateType(name, module);

            var attributeCtor = GetMigratableAttribute(c => c.Parameters.Count == 2);
            var attribute = AddMigratableAttribute(type, attributeCtor, typeHash, module);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.Import(typeof(Type)), migratorType));

            return type;
        }

        private MethodDefinition GetMigratableAttribute(Func<MethodDefinition, bool> filter)
        {
            return _MigrationDll
                .Types
                .Single(t => t.Name == "MigratableAttribute")
                .GetConstructors()
                .Single(filter);
        }

        private static CustomAttribute AddMigratableAttribute(TypeDefinition type, MethodReference attributeCtor, string typeHash, ModuleDefinition module)
        {
            var attribute = new CustomAttribute(module.Import(attributeCtor));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, typeHash));
            type.CustomAttributes.Add(attribute);
            return attribute;
        }

        private static TypeDefinition AddType(string name, ModuleDefinition module)
        {
            var type = CreateType(name, module);
            module.Types.Add(type);
            return type;
        }

        private static TypeDefinition CreateType(string name, ModuleDefinition module)
        {
            const TypeAttributes typeAttributes =
                TypeAttributes.Public
                //| TypeAttributes.AutoClass
                | TypeAttributes.AnsiClass
                | TypeAttributes.BeforeFieldInit;

            var type = new TypeDefinition("Test", name, typeAttributes) { BaseType = module.TypeSystem.Object };

            var ctor = new MethodDefinition(".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);
            var il = ctor.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, module.Import(typeof(object).GetConstructor(Type.EmptyTypes)));
            il.Emit(OpCodes.Ret);
            il.Emit(OpCodes.Ret);
            type.Methods.Add(ctor);
            return type;
        }

        private void AddMigrationMethod(TypeDefinition type, int version)
        {
            var method = new MethodDefinition("Migrate_" + version,
                MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, type.Module.TypeSystem.Void);
            method.Parameters.Add(
                new ParameterDefinition(type.Module.Import(_JsonNetDll.Types.Single(t => t.Name == "JObject"))));
            type.Methods.Add(method);

            var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ret);
        }

        private static void RunInStaThread(Action action)
        {
            RunAndGetInStaThread(() =>
            {
                action();
                return 0;
            });
        }

        private static T RunAndGetInStaThread<T>(Func<T> func)
        {
            var result = default(T);
            var thread = new Thread(() => result = func());
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return result;
        }

        public void Dispose()
        {
            foreach (var file in _CreatedTestFiles)
            {
                File.Delete(file);
            }
        }
    }
}
