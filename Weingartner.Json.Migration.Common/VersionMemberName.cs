using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Weingartner.Json.Migration.Common
{
    public static class VersionMemberName
    {
        public static string VersionPropertyName { get { return "Version"; } }
        public static string VersionBackingFieldName { get { return "_version"; } }

        public static IEnumerable<string> SupportedVersionPropertyNames
        {
            get
            {
                yield return VersionPropertyName;
                yield return "<>Version";
            }
        }

        public static IEnumerable<string> SupportedVersionBackingFieldNames
        {
            get
            {
                yield return VersionBackingFieldName;
                yield return "<>_version";
            }
        } 
    }
}
