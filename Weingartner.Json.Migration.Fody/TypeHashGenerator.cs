using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration.Fody
{
    public class TypeHashGenerator : IGenerateTypeHashes
    {
        private readonly Action<string> _Log;

        public TypeHashGenerator(Action<string> log)
        {
            _Log = log;
        }

        public string GenerateHash(TypeDefinition type)
        {
            using (var shaManager = new SHA1Managed())
            {
                var hashBase = GenerateHashBase(type);
                var hashBytes = shaManager.ComputeHash(Encoding.UTF8.GetBytes(hashBase));
                var hash = hashBytes.Aggregate(String.Empty, (current, bit) => current + bit.ToString("x2"));
                return hash;
            }
        }

        public string GenerateHashBase(TypeDefinition type)
        {
            var result = GenerateHashBaseInternal(type, new List<TypeReference>());
            return Regex.Replace(result, @"^[^(]*\((.*)\)$", "$1");
        }

        private string GenerateHashBaseInternal(TypeReference type, ICollection<TypeReference> processedTypes)
        {
            _Log("=== TypeHashGenerator: Processing " + type.FullName);
            if (type.IsGenericParameter)
            {
                return type.FullName;
            }

            var typeDef = type.Resolve();
            if (IsSimpleType(typeDef) || processedTypes.Contains(type, TypeReferenceEqualityComparer.Default))
            {
                return type.FullName;
            }

            processedTypes.Add(type);

            var genericInstance = type as GenericInstanceType;
            
            if (IsEnumerable(type))
            {
                var genericArguments = Enumerable.Empty<string>();
                if (genericInstance != null)
                {
                    genericArguments = genericInstance
                        .GenericArguments
                        .Select(argType => GenerateHashBaseInternal(argType, processedTypes));
                }

                return string.Format(
                    "{0}({1})",
                    typeDef.FullName,
                    string.Join("|", genericArguments));
            }

            var dataContractAttribute = type.Module.Import(typeof(DataContractAttribute));
            var dataMemberAttribute = type.Module.Import(typeof(DataMemberAttribute));
            var items = typeDef
                .Properties
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic)
                .Where(p => !VersionMemberName.SupportedVersionPropertyNames.Contains(p.Name))
                .Where(p =>
                    !p.DeclaringType.CustomAttributes
                        .Select(a => a.AttributeType)
                        .Any(t => t.IsProbablyEqualTo(dataContractAttribute))
                    || p.CustomAttributes
                        .Select(a => a.AttributeType)
                        .Any(t => t.IsProbablyEqualTo(dataMemberAttribute)))
                .Select(p =>
                {
                    TypeReference propertyType;
                    if (p.PropertyType.IsGenericParameter)
                    {
                        Debug.Assert(genericInstance != null);
                        var index = p.DeclaringType.GenericParameters.IndexOf(((GenericParameter)p.PropertyType));
                        propertyType = genericInstance.GenericArguments[index];
                    }
                    else
                    {
                        propertyType = p.PropertyType;
                    }
                    return string.Format(
                        "{0}-{1}",
                        GenerateHashBaseInternal(propertyType, processedTypes), p.Name);
                })
                .OrderBy(p => p);

            return string.Format("{0}({1})", typeDef.FullName, string.Join("|", items));
        }

        private static readonly List<Type> SimpleTypes = new List<Type>
        {
            typeof(bool), typeof(byte), typeof(sbyte), typeof(char), typeof(decimal),
            typeof(double), typeof(float), typeof(int), typeof(uint), typeof(long),
            typeof(ulong), typeof(short), typeof(ushort), typeof(string)
        };

        private static bool IsSimpleType(TypeDefinition type)
        {
            return SimpleTypes
                .Select(t => type.Module.Import(t).Resolve())
                .Any(t => t.IsProbablyEqualTo(type));
        }

        private static bool IsEnumerable(TypeReference type)
        {
            return type.HasInterface(type.Module.Import(typeof(System.Collections.IEnumerable)).Resolve());
        }
    }
}