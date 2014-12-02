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
        private readonly ConcurrentBag<string> _CreatedTestFiles = new ConcurrentBag<string>();

        private const string EmptyTypeHash = "da39a3ee5e6b4b0d3255bfef95601890afd80709";

        private readonly ModuleDefinition _JsonNetDll;
        private readonly ModuleDefinition _MigrationDll;

        public ModuleWeaverSpec()
        {
            const string jsonNetDllPath = @"..\..\..\packages\Newtonsoft.Json.6.0.6\lib\net45\Newtonsoft.Json.dll";
            File.Copy(jsonNetDllPath, Path.GetFileName(jsonNetDllPath), true);
            _CreatedTestFiles.Add(Path.GetFileName(jsonNetDllPath));
            _JsonNetDll = ModuleDefinition.ReadModule(Path.GetFileName(jsonNetDllPath));

            var migrationDllPath = string.Format(@"..\..\..\Weingartner.Json.Migration\bin\{0}\Weingartner.Json.Migration.dll", Configuration);
            File.Copy(migrationDllPath, Path.GetFileName(migrationDllPath), true);
            _CreatedTestFiles.Add(Path.GetFileName(migrationDllPath));
            _MigrationDll = ModuleDefinition.ReadModule(Path.GetFileName(migrationDllPath));
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
            string oldAssemblyPath, newAssemblyPath;
            WeaveValidAssembly(out oldAssemblyPath, out newAssemblyPath);

            Verifier.Verify(oldAssemblyPath, newAssemblyPath);

            newAssembly = Assembly.LoadFrom(Path.GetFullPath(newAssemblyPath));
        }

        private void WeaveValidAssembly(out string oldAssemblyPath, out string newAssemblyPath)
        {
            var guid = Guid.NewGuid();

            var module = CreateTestModule();

            var testDataType = AddMigratableType("TestData", EmptyTypeHash, module, _MigrationDll);
            AddMigrationMethod(testDataType, 1, _JsonNetDll);

            var testDataContractType = AddMigratableType("TestDataContract", EmptyTypeHash, module, _MigrationDll);
            testDataContractType.CustomAttributes.Add(new CustomAttribute(module.Import(typeof(DataContractAttribute).GetConstructor(Type.EmptyTypes))));
            AddMigrationMethod(testDataContractType, 1, _JsonNetDll);

            AddMigratableType("TestDataWithoutMigration", EmptyTypeHash, module, _MigrationDll);

            var topLevelType = AddType("TopLevelType", module);
            var nestedType = CreateMigratableType("NestedType", EmptyTypeHash, module, _MigrationDll);
            nestedType.Namespace = string.Empty;
            nestedType.Attributes &= ~TypeAttributes.Public;
            nestedType.Attributes |= TypeAttributes.NestedPublic;
            topLevelType.NestedTypes.Add(nestedType);

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

            AddMigratableType("TestData", "wronghash", module, _MigrationDll);

            var weavingTask = new ModuleWeaver { ModuleDefinition = module };
            weavingTask.Execute();
        }

        private void WeaveTypeWithNonConsecutiveMigrationMethods()
        {
            var module = CreateTestModule();

            var type = AddMigratableType("TestData", EmptyTypeHash, module, _MigrationDll);
            AddMigrationMethod(type, 1, _JsonNetDll);
            AddMigrationMethod(type, 2, _JsonNetDll);
            AddMigrationMethod(type, 4, _JsonNetDll);

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

        private static TypeDefinition AddMigratableType(string name, string typeHash, ModuleDefinition module, ModuleDefinition migrationDll)
        {
            var type = CreateMigratableType(name, typeHash, module, migrationDll);
            module.Types.Add(type);
            return type;
        }

        private static TypeDefinition CreateMigratableType(string name, string typeHash, ModuleDefinition module, ModuleDefinition migrationDll)
        {
            var type = CreateType(name, module);

            var attributeCtor = module.Import(migrationDll.Types.Single(t => t.Name == "MigratableAttribute").GetConstructors().Single());
            var attribute = new CustomAttribute(attributeCtor);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, typeHash));
            type.CustomAttributes.Add(attribute);

            return type;
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
            foreach (var file in _CreatedTestFiles)
            {
                File.Delete(file);
            }
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
    }
}
