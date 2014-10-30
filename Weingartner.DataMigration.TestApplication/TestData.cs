using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Weingartner.DataMigration.TestApplication
{
    // ReSharper disable UnusedMember.Local
    // ReSharper disable UnusedParameter.Local
    [Migratable("da39a3ee5e6b4b0d3255bfef95601890afd80709")]
    public class TestData
    {
        private static void Migrate_0(JObject data) { }
    }

    [DataContract]
    [Migratable("da39a3ee5e6b4b0d3255bfef95601890afd80709")]
    public class TestDataContract
    {
        private static void Migrate_0(JObject data) { }
    }

    [Migratable("da39a3ee5e6b4b0d3255bfef95601890afd80709")]
    public class TestDataWithoutMigration { }
    // ReSharper restore UnusedParameter.Local
    // ReSharper restore UnusedMember.Local
}
