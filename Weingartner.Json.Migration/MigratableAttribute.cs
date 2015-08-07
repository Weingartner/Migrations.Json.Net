using System;

namespace Weingartner.Json.Migration
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class MigratableAttribute : Attribute
    {
        public string TypeHash { get; }

        public MigratableAttribute(string typeHash)
        {
            TypeHash = typeHash;
        }
    }
}
