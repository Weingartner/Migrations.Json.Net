using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Weingartner.Json.Migration.Common
{
    public class MigrationMethodVerifier
    {
        private readonly Func<SimpleType, SimpleType, bool> _IsSuperType;
        private static readonly SimpleType JTokenType = new SimpleType("Newtonsoft.Json.Linq.JToken", new AssemblyName("Newtonsoft.Json, Version=7.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed"));
        private static readonly SimpleType JsonSerializerType = new SimpleType("Newtonsoft.Json.JsonSerializer", new AssemblyName("Newtonsoft.Json, Version=7.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed"));

        public MigrationMethodVerifier(Func<SimpleType, SimpleType, bool> isSuperType)
        {
            _IsSuperType = isSuperType;
        }

        /// <summary>
        /// Checks the signiture of a migration method. 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="jsonArgumentType">Can be null if you don't want to check the type. Otherwise it should be one of JObject or JArray</param>
        public void VerifyMigrationMethodSignature(MigrationMethod method, SimpleType jsonArgumentType)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            if (jsonArgumentType != null && !_IsSuperType(JTokenType, jsonArgumentType))
                ThrowInvalidMigrationSignature(method, jsonArgumentType);

            if (method.Parameters.Count != 2)
                ThrowInvalidMigrationSignature(method, jsonArgumentType);

            if (jsonArgumentType != null && !_IsSuperType(method.Parameters[0].Type, jsonArgumentType))
                ThrowInvalidMigrationSignature(method, jsonArgumentType);

            if (!_IsSuperType(JsonSerializerType, method.Parameters[1].Type))
                ThrowInvalidMigrationSignature(method, jsonArgumentType);

            if (!_IsSuperType(JTokenType, method.ReturnType))
                ThrowInvalidMigrationSignature(method, jsonArgumentType);
        }

        private static void ThrowInvalidMigrationSignature(MigrationMethod method, SimpleType parameterType)
        {
            Debug.Assert(method.DeclaringType != null, "method.DeclaringType != null");

            var builder = new StringBuilder();
            builder.AppendLine($"Migration method '{method.FullName}' should have the following signature:");
            builder.AppendLine($"private static {JTokenType.Name} {method.Name}({parameterType?.Name ?? JTokenType.Name} data, {JsonSerializerType.Name} serializer)");
            throw new MigrationException(builder.ToString());
        }
    }
}
