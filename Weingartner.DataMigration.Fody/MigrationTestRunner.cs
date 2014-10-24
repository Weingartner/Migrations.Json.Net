using System.Linq;
using Mono.Cecil;
using Weingartner.DataMigration.Common;

namespace Weingartner.DataMigration.Fody
{
    public class MigrationTestRunner : AbstractMigration<object, TypeDefinition, MethodDefinition>
    {
        protected override string ExtractHash(object data)
        {
            return string.Empty;
        }

        protected override string GetCurrentVersion(TypeDefinition type)
        {
            return new TypeHashGenerator().GenerateHash(type);
        }

        protected override MethodDefinition GetMigrationMethod(TypeDefinition type, string version)
        {
            var methods = type.Methods
                .Where(x => x.IsStatic && !x.IsPublic)
                .Where(x => x.CustomAttributes
                    .Where(y => y.AttributeType.FullName == "Weingartner.DataMigration.MigrationAttribute") // TODO implement proper equality method
                    .Any(y => (string)y.ConstructorArguments[0].Value == version))
                .ToList();

            if (methods.Count > 1)
            {
                ThrowMultipleMigrationMethodsException(type, version);
            }

            return methods.FirstOrDefault();
        }

        protected override string GetTargetMigrationVersion(MethodDefinition method)
        {
            return (string)method.CustomAttributes
                .Single(a => a.AttributeType.FullName == "Weingartner.DataMigration.MigrationAttribute")
                .ConstructorArguments[1].Value;
        }

        protected override void ExecuteMigration(MethodDefinition method, ref object data)
        {
            // Nothing to do
        }

        protected override void CheckParameters(MethodDefinition method)
        {
            var parameters = method.Parameters;
            if (parameters.Count != 1 || parameters[0].ParameterType.FullName != "Newtonsoft.Json.Linq.JObject")
            {
                ThrowInvalidParametersException(GetTypeName(method.DeclaringType), method.Name);
            }
        }

        protected override string GetTypeName(TypeDefinition type)
        {
            return type.FullName;
        }
    }
}
