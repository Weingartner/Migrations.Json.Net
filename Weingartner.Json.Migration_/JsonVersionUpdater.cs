using System.Linq;
using Newtonsoft.Json.Linq;

namespace Weingartner.Json.Migration
{
    public class JsonVersionUpdater : IUpdateVersions<JToken>
    {
        public int GetVersion(JToken data)
        {
            if (!(data is JObject)) return 0;

            var versionToken = VersionMemberName.SupportedVersionPropertyNames
                .Select(n => data[n])
                .FirstOrDefault(x => x != null);
            return versionToken?.Value<int>() ?? 0;
        }

        public void SetVersion(JToken data, int version)
        {
            if (!(data is JObject)) return;
            if (data.First.ToString().Contains("$ref"))
                return;
            data[VersionMemberName.VersionPropertyName] = version;
        }
    }
}
