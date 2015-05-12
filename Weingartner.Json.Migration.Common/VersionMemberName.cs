using System;

namespace Weingartner.Json.Migration.Common
{
    public static class VersionMemberName
    {
        public static string VersionPropertyName { get { return "Version"; } }
        public static string VersionBackingFieldName { get { return "_version"; } }
    }
}
