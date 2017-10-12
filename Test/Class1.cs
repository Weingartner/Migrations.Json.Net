using System;
using System.Runtime.Serialization;
using Weingartner.Json.Migration;
using Weingartner.Json.Migration.Common;

namespace Test
{
    [Migratable("758832613")]
    [DataContract]
    public class Class1
    {
        [DataMember]
        private int i { get; set; }
    }
}
