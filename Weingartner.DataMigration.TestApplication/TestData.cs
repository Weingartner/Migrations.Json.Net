using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Weingartner.DataMigration.TestApplication
{
    // ReSharper disable UnusedMember.Local
    // ReSharper disable UnusedParameter.Local
    [Migratable]
    public class TestData
    {
        [Migration("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        private static void Migrate_0(JObject data) { }

        public void Test() { }
    }

    [DataContract]
    [Migratable]
    public class TestDataContract
    {
        [Migration("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        private static void Migrate_0(JObject data) { }
    }
    // ReSharper restore UnusedParameter.Local
    // ReSharper restore UnusedMember.Local
}
