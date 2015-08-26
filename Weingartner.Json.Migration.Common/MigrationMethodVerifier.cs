using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Weingartner.Json.Migration.Common
{
    public class MigrationMethodVerifier
    {
        private readonly Func<SimpleType, SimpleType, bool> _CanAssign;
        internal static readonly SimpleType JTokenType = new SimpleType("Newtonsoft.Json.Linq.JToken", new AssemblyName("Newtonsoft.Json, Version=7.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed"));
        internal static readonly SimpleType JsonSerializerType = new SimpleType("Newtonsoft.Json.JsonSerializer", new AssemblyName("Newtonsoft.Json, Version=7.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed"));

        public MigrationMethodVerifier(Func<SimpleType, SimpleType, bool> canAssign)
        {
            _CanAssign = canAssign;
        }

        public IEnumerable<VerificationResult> VerifyMigrationMethods(IReadOnlyList<MigrationMethod> migrationMethods)
        {
            var firstVersion = migrationMethods.FirstOrDefault()?.ToVersion ?? 1;
            if (firstVersion != 1)
                yield return new VerificationResult(migrationMethods[0], VerificationResultEnum.DoesntStartWithOne, null);

            var nonConsecutive = migrationMethods
                .Zip(migrationMethods.Skip(1), (x, y) => new { Previous = x, Current = y })
                .Where(pair => pair.Previous.ToVersion != pair.Current.ToVersion - 1)
                .Select(pair => pair.Current)
                .Select(method => new VerificationResult(method, VerificationResultEnum.IsNotConsecutive, null));
            foreach (var result in nonConsecutive)
            {
                yield return result;
            }

            var withPreviousReturnType = migrationMethods
                .Select(x => new {MigrationMethod = x, x.ReturnType})
                .ToList();

            var invalidMigrationMethods = new[] { new { MigrationMethod = (MigrationMethod) null, ReturnType = (SimpleType) null } }
                .Concat(withPreviousReturnType)
                .Zip(withPreviousReturnType, (x, y) => new {Previous = x, Current = y})
                .Select(x => new VerificationResult(
                    x.Current.MigrationMethod,
                    VerifyMigrationMethodSignature(x.Current.MigrationMethod, x.Previous.ReturnType),
                    x.Previous.ReturnType)
                );

            foreach (var result in invalidMigrationMethods)
            {
                yield return result;
            }
        }

        /// <summary>
        /// Checks the signiture of a migration method. 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="jsonArgumentType">Can be null if you don't want to check the type. Otherwise it should be one of JObject or JArray</param>
        public VerificationResultEnum VerifyMigrationMethodSignature(MigrationMethod method, SimpleType jsonArgumentType)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            if (!_CanAssign(method.Parameters[0].Type, JTokenType))
                return VerificationResultEnum.FirstArgumentMustBeAssignableToJToken;

            if (method.Parameters.Count != 2)
                return VerificationResultEnum.ParameterCountDoesntMatch;

            if (jsonArgumentType != null && !_CanAssign(jsonArgumentType, method.Parameters[0].Type))
                return VerificationResultEnum.FirstArgumentMustBeAssignableToReturnTypeOfPreviousMigrationMethod;

            if (!_CanAssign(method.Parameters[1].Type, JsonSerializerType))
                return VerificationResultEnum.SecondArgumentMustBeAssignableToJsonSerializer;

            if (!_CanAssign(method.ReturnType, JTokenType))
                return VerificationResultEnum.ReturnTypeMustBeAssignableToJToken;

            return VerificationResultEnum.Ok;
        }
    }

    public class VerificationResult
    {
        private readonly SimpleType _ExpectedDataArgumentType;
        public MigrationMethod Method { get; }
        public VerificationResultEnum Result { get; }

        public VerificationResult(MigrationMethod method, VerificationResultEnum result, SimpleType expectedDataArgumentType)
        {
            _ExpectedDataArgumentType = expectedDataArgumentType;
            Method = method;
            Result = result;
        }

        public void ThrowIfInvalid()
        {
            Debug.Assert(Method.DeclaringType != null, "method.DeclaringType != null");

            if (Result == VerificationResultEnum.Ok) return;

            if (Result == VerificationResultEnum.DoesntStartWithOne)
                throw new MigrationException($"Migrations of type '{Method.DeclaringType.FullName}' must start with '{MigrationMethod.NamePrefix}1.");

            if (Result == VerificationResultEnum.IsNotConsecutive)
                throw new MigrationException($"Migrations of type '{Method.DeclaringType.FullName}' must be consecutive.");

            if (Result != VerificationResultEnum.Ok)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Migration method '{Method.FullName}' should have the following signature:");
                var jtoken = MigrationMethodVerifier.JTokenType.Name;
                var jsonSerializer = MigrationMethodVerifier.JsonSerializerType.Name;
                var argumentType = _ExpectedDataArgumentType?.Name ?? MigrationMethodVerifier.JTokenType.Name;
                builder.AppendLine($"private static {jtoken} {Method.Name}({argumentType} data, {jsonSerializer} serializer)");
                throw new MigrationException(builder.ToString());
            }
        }
    }

    public enum VerificationResultEnum
    {
        Ok,
        FirstArgumentMustBeAssignableToJToken,
        ParameterCountDoesntMatch,
        FirstArgumentMustBeAssignableToReturnTypeOfPreviousMigrationMethod,
        SecondArgumentMustBeAssignableToJsonSerializer,
        ReturnTypeMustBeAssignableToJToken,
        DoesntStartWithOne,
        IsNotConsecutive
    }
}
