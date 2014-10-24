using Newtonsoft.Json.Linq;

namespace Weingartner.DataMigration
{
    public class JObjectHashExtractor : IExtractHashes<JObject>
    {
        public string ExtractHash(JObject data)
        {
            var versionToken = data["Version"];
            return versionToken != null && versionToken.Value<string>() != null
                ? versionToken.Value<string>()
                : string.Empty;
        }
    }
}
