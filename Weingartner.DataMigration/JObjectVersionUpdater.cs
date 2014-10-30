using Newtonsoft.Json.Linq;
using Weingartner.DataMigration.Common;

namespace Weingartner.DataMigration
{
    public class JObjectVersionUpdater : IUpdateVersions<JObject>
    {
        public int GetVersion(JObject data)
        {
            var versionToken = data[Globals.VersionPropertyName];
            return versionToken != null
                ? versionToken.Value<int>()
                : 0;
        }


        public void SetVersion(JObject data, int version)
        {
            data[Globals.VersionPropertyName] = version;
        }
    }
}
