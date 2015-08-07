using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Weingartner.Json.Migration.Common
{
    public class MigrationMethod
    {
        public const string NamePrefix = "Migrate_";
        private static readonly Regex MigrationMethodPattern = new Regex($@"^{NamePrefix}(?<toVersion>\d+)$");

        public static MigrationMethod TryParse(SimpleType declaringType, IReadOnlyList<MethodParameter> parameters,
            SimpleType returnType, string name)
        {
            var match = MigrationMethodPattern.Match(name);
            if (!match.Success) return null;

            var toVersion = int.Parse(match.Groups["toVersion"].Value);
            return new MigrationMethod(declaringType, parameters, returnType, toVersion);
        }

        private MigrationMethod(SimpleType declaringType, IReadOnlyList<MethodParameter> parameters,
            SimpleType returnType, int toVersion)
        {
            Parameters = parameters;
            ToVersion = toVersion;
            ReturnType = returnType;
            DeclaringType = declaringType;
        }

        public SimpleType ReturnType { get; }
        public int ToVersion { get; }
        public IReadOnlyList<MethodParameter> Parameters { get; }
        public SimpleType DeclaringType { get; }
        public string Name => NamePrefix + ToVersion;
        public string FullName => $"{DeclaringType.Name}.{NamePrefix}{ToVersion}";
    }
}