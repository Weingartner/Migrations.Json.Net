using System;

namespace Weingartner.Json.Migration
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MigratableAttribute : Attribute
    {
        public string TypeHash { get; private set; }
        public Type MigratorType { get; private set; }

        public MigratableAttribute(string typeHash)
        {
            TypeHash = typeHash;
        }

        public MigratableAttribute(string typeHash, Type migratorType)
            : this(typeHash)
        {
            MigratorType = migratorType;
        }
    }
}
