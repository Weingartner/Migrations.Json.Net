using System;

namespace Weingartner.Json.Migration
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MigratableAttribute : Attribute
    {
        public string TypeHash { get; private set; }

        public MigratableAttribute(string typeHash)
        {
            TypeHash = typeHash;
        }

    }
}
