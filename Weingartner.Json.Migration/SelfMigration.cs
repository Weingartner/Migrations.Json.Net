using System;
using System.Reflection;

namespace Weingartner.Json.Migration
{
    internal class SelfMigration<TSerializedData> : IMigrator<TSerializedData>
    {
        private readonly Type _DataType;

        public SelfMigration(Type dataType)
        {
            if (dataType == null) throw new ArgumentNullException("dataType");
            _DataType = dataType;
        }

        public void MigrateData(ref TSerializedData data, int toVersion)
        {
            var migrationMethod = GetMigrationMethod(toVersion);
            if (migrationMethod == null)
            {
                throw new MigrationException(
                    string.Format(
                        "The migration method, which migrates an instance of type '{0}' to version {1} cannot be found. " +
                        "To resolve this, add a method with the following signature: `private static void Migrate_{1}(ref {2} data)",
                        _DataType.FullName,
                        toVersion,
                        typeof(TSerializedData).FullName));
            }

            CheckParameters(migrationMethod, data.GetType());

            ExecuteMigration(migrationMethod, ref data); 
        }

        private MethodInfo GetMigrationMethod(int toVersion)
        {
            return _DataType.GetMethod("Migrate_" + toVersion, BindingFlags.Static | BindingFlags.NonPublic);
        }

        private void CheckParameters(MethodBase method, Type dataType)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 1 || !parameters[0].ParameterType.IsAssignableFrom(dataType.MakeByRefType()))
            {
                // ReSharper disable once PossibleNullReferenceException
                ThrowInvalidParametersException(method.DeclaringType.FullName, method.Name);
            }
        }

        protected void ThrowInvalidParametersException(string typeName, string methodName)
        {
            throw new MigrationException(
                string.Format(
                    "Migration method '{0}.{1}' must have a single parameter of type '{2}'.",
                    typeName,
                    methodName,
                    typeof(TSerializedData).FullName));
        }

        private static void ExecuteMigration(MethodInfo method, ref TSerializedData data)
        {
            var parameters = new object[] { data };
            method.Invoke(null, parameters);
            data = (TSerializedData)parameters[0];
        }
    }
}