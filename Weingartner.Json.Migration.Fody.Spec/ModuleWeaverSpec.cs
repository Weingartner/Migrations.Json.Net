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

namespace Weingartner.Json.Migration.Fody.Spec
{
    public class ModuleWeaverSpec : IDisposable
    {
        private readonly ConcurrentBag<string> _CreatedFiles = new ConcurrentBag<string>();

        private const string EmptyTypeHash = "da39a3ee5e6b4b0d3255bfef95601890afd80709";
            
        [Fact]
        public void PeVerify()
        {
            string newAssemblyPath;
            string assemblyPath;
            WeaveValidAssembly(out newAssemblyPath, out assemblyPath);

            Verifier.Verify(assemblyPath, newAssemblyPath);
        }

        [Fact]
        public void ShouldInjectVersionProperty()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestData");
            var instance = Activator.CreateInstance(type);

            var property = instance.GetType().GetProperty(VersionMemberName.Instance.VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            property.Should().NotBeNull();
        }

        [Fact]
        public void ShouldInjectCorrectVersionInTypeThatHasMigrationMethods()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestData");
            var instance = Activator.CreateInstance(type);

            var property = instance.GetType().GetProperty(VersionMemberName.Instance.VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            property.GetValue(instance).Should().Be(1);
        }

        [Fact]
        public void ShouldInjectCorrectVersionInTypeThatHasNoMigrationMethods()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestDataWithoutMigration");
            var instance = Activator.CreateInstance(type);

            var property = instance.GetType().GetProperty(VersionMemberName.Instance.VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            property.GetValue(instance).Should().Be(0);
        }

        [Fact]
        public void ShouldInjectDataMemberAttributeIfTypeHasDataContractAttribute()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestDataContract");

            type.GetProperty(VersionMemberName.Instance.VersionPropertyName)
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
            ((int)type.GetField(VersionMemberName.Instance.VersionBackingFieldName, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).Should().Be(0);
        }

        [Fact]
        public void ShouldCreateConstVersionField()
        {
            Assembly assembly;
            WeaveValidAssembly(out assembly);
            var type = assembly.GetType("Test.TestData");
            var instance = Activator.CreateInstance(type);

            var field = instance.GetType().GetField(VersionMemberName.Instance.VersionBackingFieldName, BindingFlags.Static | BindingFlags.NonPublic);
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
        public void ShouldHaveMigrationMethodSignatureInClipboardWhenMigrationMethodMightBeNeeded()
        {
            try
            {
                Clipboard.Clear();
                WeaveTypeWithWrongHash();
            }
            catch (MigrationException) { }
            finally
            {
                Clipboard.GetText().Should().NotBeEmpty();
            }
        }

        [Fact]
        public void ShouldThrowWhenMigratableTypeHasNonConsecutiveMigrationMethods()
        {
            new Action(WeaveTypeWithNonConsecutiveMigrationMethods)
                .ShouldThrow<MigrationException>()
                .Where(e => e.Message.Contains("there is no migration to version"));
        }

        [Fact]
        public void ShouldPutTextToClipboardWhenCalledFromNonStaThread()
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
            thread.Start();
            thread.Join();

            clipboardText.Should().NotBeEmpty();
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

        private void WeaveValidAssembly(out Assembly newAssembly)
        {
            string newAssemblyPath, _;
            WeaveValidAssembly(out _, out newAssemblyPath);
            newAssembly = Assembly.LoadFrom(Path.GetFullPath(newAssemblyPath));
        }

        private void WeaveValidAssembly(out string oldAssemblyPath, out string newAssemblyPath)
        {
            var guid = Guid.NewGuid();

            ModuleDefinition jsonNetDll;
            ModuleDefinition migrationDll;
            var module = CreateTestModule(guid, out jsonNetDll, out migrationDll);

            var testDataType = AddMigratableType("TestData", EmptyTypeHash, module, migrationDll);
            AddMigrationMethod(testDataType, 1, jsonNetDll);

            var testDataContractType = AddMigratableType("TestDataContract", EmptyTypeHash, module, migrationDll);
            testDataContractType.CustomAttributes.Add(new CustomAttribute(module.Import(typeof(DataContractAttribute).GetConstructor(Type.EmptyTypes))));
            AddMigrationMethod(testDataContractType, 1, jsonNetDll);

            AddMigratableType("TestDataWithoutMigration", EmptyTypeHash, module, migrationDll);

            oldAssemblyPath = string.Format("Test.old.{0}.dll", guid);
            module.Write(oldAssemblyPath);

            var weavingTask = new ModuleWeaver { ModuleDefinition = module };
            weavingTask.Execute();

            newAssemblyPath = string.Format("Test.new.{0}.dll", guid);
            module.Write(newAssemblyPath);
        }

        private void WeaveTypeWithWrongHash()
        {
            ModuleDefinition jsonNetDll;
            ModuleDefinition migrationDll;
            var module = CreateTestModule(Guid.NewGuid(), out jsonNetDll, out migrationDll);

            AddMigratableType("TestData", "wronghash", module, migrationDll);

            var weavingTask = new ModuleWeaver { ModuleDefinition = module };
            weavingTask.Execute();
        }

        private void WeaveTypeWithNonConsecutiveMigrationMethods()
        {
            ModuleDefinition jsonNetDll;
            ModuleDefinition migrationDll;
            var module = CreateTestModule(Guid.NewGuid(), out jsonNetDll, out migrationDll);

            var type = AddMigratableType("TestData", EmptyTypeHash, module, migrationDll);
            AddMigrationMethod(type, 1, jsonNetDll);
            AddMigrationMethod(type, 2, jsonNetDll);
            AddMigrationMethod(type, 4, jsonNetDll);

            var weavingTask = new ModuleWeaver { ModuleDefinition = module };
            weavingTask.Execute(); 
        }

        private ModuleDefinition CreateTestModule(Guid guid, out ModuleDefinition jsonNetDll, out ModuleDefinition migrationDll)
        {
            var module = ModuleDefinition.CreateModule("Test", ModuleKind.Dll);

            const string origJsonNetDllPath = @"..\..\..\packages\Newtonsoft.Json.6.0.5\lib\net45\Newtonsoft.Json.dll";
            var jsonNetDllPath = string.Format("{0}.{1}{2}", Path.GetFileNameWithoutExtension(origJsonNetDllPath), guid, Path.GetExtension(origJsonNetDllPath));
            File.Copy(origJsonNetDllPath, jsonNetDllPath);
            _CreatedFiles.Add(jsonNetDllPath);
            jsonNetDll = ModuleDefinition.ReadModule(Path.GetFileName(jsonNetDllPath));
            module.AssemblyReferences.Add(jsonNetDll.Assembly.Name);

            const string origMigrationDllPath = @"..\..\..\Weingartner.Json.Migration\bin\Debug\Weingartner.Json.Migration.dll";
            var migrationDllPath = string.Format("{0}.{1}{2}", Path.GetFileNameWithoutExtension(origMigrationDllPath), guid, Path.GetExtension(origMigrationDllPath));
            File.Copy(origMigrationDllPath, migrationDllPath);
            _CreatedFiles.Add(migrationDllPath);
            migrationDll = ModuleDefinition.ReadModule(Path.GetFileName(migrationDllPath));
            module.AssemblyReferences.Add(migrationDll.Assembly.Name);
            return module;
        }

        private static TypeDefinition AddMigratableType(string name, string typeHash, ModuleDefinition module, ModuleDefinition migrationDll)
        {
            const TypeAttributes typeAttributes =
                TypeAttributes.Public
                | TypeAttributes.AutoClass
                | TypeAttributes.AnsiClass
                | TypeAttributes.BeforeFieldInit;

            var type = new TypeDefinition("Test", name, typeAttributes) { BaseType = module.TypeSystem.Object };

            var attributeCtor = module.Import(migrationDll.Types.Single(t => t.Name == "MigratableAttribute").GetConstructors().Single());
            var attribute = new CustomAttribute(attributeCtor);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, typeHash));
            type.CustomAttributes.Add(attribute);

            var ctor = new MethodDefinition(".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);
            var il = ctor.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, module.Import(typeof(object).GetConstructor(Type.EmptyTypes)));
            il.Emit(OpCodes.Ret);
            type.Methods.Add(ctor);

            module.Types.Add(type);

            return type;
        }

        private static void AddMigrationMethod(TypeDefinition type, int version, ModuleDefinition jsonNetDll)
        {
            var method = new MethodDefinition("Migrate_" + version,
                MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, type.Module.TypeSystem.Void);
            method.Parameters.Add(
                new ParameterDefinition(type.Module.Import(jsonNetDll.Types.Single(t => t.Name == "JObject"))));
            type.Methods.Add(method);

            var il = method.Body.GetILProcessor();
            il.Emit(OpCodes.Ret);
        }

        public void Dispose()
        {
            foreach (var file in _CreatedFiles)
            {
                File.Delete(file);
            }
        }
    }
}
