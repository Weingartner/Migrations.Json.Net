using System;

namespace Weingartner.DataMigration
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class MigratableAttribute : Attribute
    {
    }
}
