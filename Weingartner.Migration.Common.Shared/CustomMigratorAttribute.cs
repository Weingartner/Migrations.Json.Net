using System;

namespace Weingartner.Json.Migration
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class CustomMigratorAttribute : Attribute
    {
        public Type MigratorType { get; }

        public CustomMigratorAttribute(Type migratorType)
        {
            MigratorType = migratorType;
        }
    }
}
