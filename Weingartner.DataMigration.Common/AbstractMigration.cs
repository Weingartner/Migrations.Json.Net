using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Weingartner.DataMigration.Common
{
    public abstract class AbstractMigration<TData, TType, TMethod> : IMigrateData<TData, TType>
        where TData : class
        where TType : class
        where TMethod : class
    {
        public void Migrate(ref TData data, TType dataType)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (dataType == null) throw new ArgumentNullException("dataType");

            var dataVersion = ExtractHash(data);
            var currentVersion = GetCurrentVersion(dataType);

            var processedDataVersions = new List<string> { dataVersion };

            while (dataVersion != currentVersion)
            {
                var migrationMethod = GetMigrationMethod(dataType, dataVersion);
                if (migrationMethod == null)
                {
                    throw new MigrationException(
                        string.Format(
                            "Missing method to migrate instance of type '{0}' from version '{1}'. " +
                            "Method must be private and static. " +
                            "If the method migrates the data to the latest version, " +
                            "add the custom attribute '[Migration(\"{1}\", \"{2}\")'",
                            GetTypeName(dataType),
                            dataVersion,
                            currentVersion));
                }

                CheckParameters(migrationMethod);

                ExecuteMigration(migrationMethod, ref data);

                dataVersion = GetTargetMigrationVersion(migrationMethod);

                if (processedDataVersions.Contains(dataVersion))
                {
                    throw new MigrationException(
                        string.Format(
                        "Cannot migrate data of type '{0}' from version {1} to version {2} because of a circular migration.",
                        GetTypeName(dataType),
                        dataVersion,
                        currentVersion));
                }
            }
        }

        protected void ThrowInvalidParametersException(string typeName, string methodName)
        {
            throw new MigrationException(
                string.Format(
                    "Migration method '{0}.{1}' must have a single parameter of type '{2}'.",
                    typeName,
                    methodName,
                    typeof(TData).FullName));
        }

        protected void ThrowMultipleMigrationMethodsException(TType type, string version)
        {
            throw new MigrationException(
                string.Format(
                    "Multiple migration methods found, which can migrate an instance of type '{0}' from version '{1}'",
                    GetTypeName(type),
                    version
                ));
        }

        protected abstract string ExtractHash(TData data);

        protected abstract string GetCurrentVersion(TType type);

        protected abstract TMethod GetMigrationMethod(TType type, string version);

        protected abstract void CheckParameters(TMethod method);

        protected abstract void ExecuteMigration(TMethod method, ref TData data);

        protected abstract string GetTargetMigrationVersion(TMethod method);

        protected abstract string GetTypeName(TType type);
    }
}
