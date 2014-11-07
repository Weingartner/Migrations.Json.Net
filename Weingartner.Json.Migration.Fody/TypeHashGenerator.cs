using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration.Fody
{
    public class TypeHashGenerator : IGenerateTypeHashes
    {
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
            var result = GenerateHashBaseInternal(type, new List<TypeDefinition>());
            return Regex.Replace(result, @"^[^(]*\((.*)\)$", "$1");
        }

        private static string GenerateHashBaseInternal(TypeReference type, ICollection<TypeDefinition> processedTypes)
        {
            if (type.IsGenericParameter || IsSimpleType(type.Resolve()) || processedTypes.Contains(type))
            {
                return GetTypeName(type);
            }

            processedTypes.Add(type.Resolve());

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
                    GetTypeName(type),
                    string.Join("|", genericArguments));
            }
            
            var items = type.Resolve()
                .Properties
                .Where(p => p.GetMethod != null && p.GetMethod.IsPublic)
                .Where(p => p.Name != Globals.VersionPropertyName)
                // TODO if type has DataContractAttribute property must have DataMemberAttribute
                //.Where(p => p.CustomAttributes
                //    .Select(a => a.AttributeType)
                //    .Contains(typeof(DataMemberAttribute)))
                .Select(p =>
                {
                    var typee = type;
                    Console.WriteLine(typee);
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

            return string.Format("{0}({1})", GetTypeName(type), string.Join("|", items));
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

        private static string GetTypeName(TypeReference type)
        {
            return type.IsGenericParameter ? type.FullName : type.Resolve().FullName;
        }
    }
}