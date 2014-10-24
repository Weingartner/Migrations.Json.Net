using System;

namespace Weingartner.DataMigration
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class MigrationAttribute : Attribute
    {
        public MigrationAttribute(string fromVersion, string toVersion)
        {
            FromVersion = fromVersion;
            ToVersion = toVersion;
        }

        public string FromVersion { get; private set; }

        public string ToVersion { get; private set; }
    }
}