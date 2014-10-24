using System;
using System.Linq;
using System.Reflection;
using Weingartner.DataMigration.Common;

namespace Weingartner.DataMigration
{
    public class HashBasedDataMigrator<TData> : AbstractMigration<TData, Type, MethodInfo>
        where TData : class
    {
        private readonly IExtractHashes<TData> _HashExtractor;

        public HashBasedDataMigrator(IExtractHashes<TData> hashExtractor)
        {
            _HashExtractor = hashExtractor;
        }

        protected override string ExtractHash(TData data)
        {
            return _HashExtractor.ExtractHash(data);
        }

        protected override string GetCurrentVersion(Type type)
        {
// ReSharper disable once PossibleNullReferenceException
            return (string)type.GetField("VersionStatic", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        }

        protected override MethodInfo GetMigrationMethod(Type type, string version)
        {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<MigrationAttribute>().FromVersion == version)
                .ToList();

            if (methods.Count > 1)
            {
                ThrowMultipleMigrationMethodsException(type, version);
            }

            return methods.FirstOrDefault();
        }

        protected override void CheckParameters(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(TData).MakeByRefType())
            {
                ThrowInvalidParametersException(GetTypeName(method.DeclaringType), method.Name);
            }
        }

        protected override void ExecuteMigration(MethodInfo method, ref TData data)
        {
            method.Invoke(null, new object[] { data });
        }

        protected override string GetTargetMigrationVersion(MethodInfo method)
        {
            return method.GetCustomAttribute<MigrationAttribute>().ToVersion;
        }

        protected override string GetTypeName(Type type)
        {
            return type.FullName;
        }
    }
}
