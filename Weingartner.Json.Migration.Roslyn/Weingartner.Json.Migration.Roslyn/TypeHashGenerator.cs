using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Weingartner.Json.Migration.Common;

namespace Weingartner.Json.Migration.Fody
{
    //public class TypeHashGenerator
    //{
    //    public string GenerateHash(INamedTypeSymbol type)
    //    {
    //        return GenerateHashBase(type).GetHashCode().ToString();
    //    }

    //    public string GenerateHashBase(INamedTypeSymbol type)
    //    {
    //        var result = GenerateHashBaseInternal(type, new List<string>());
    //        return Regex.Replace(result, @"^[^(]*\((.*)\)$", "$1");
    //    }

    //    private string GenerateHashBaseInternal(ITypeSymbol type, ICollection<string> processedTypes)
    //    {
    //        if (type.Kind == SymbolKind.TypeParameter)
    //        {
    //            return type.Name;
    //        }

    //        if (IsSimpleType(type) || processedTypes.Contains(type.Name))
    //        {
    //            return type.Name;
    //        }

    //        processedTypes.Add(type.Name);

    //        var genericInstance = type as GenericInstanceType;

    //        if (IsEnumerable(type))
    //        {
    //            var genericArguments = Enumerable.Empty<string>();
    //            if (genericInstance != null)
    //            {
    //                genericArguments = genericInstance
    //                    .GenericArguments
    //                    .Select(argType => GenerateHashBaseInternal(argType, processedTypes));
    //            }

    //            return string.Format(
    //                "{0}({1})",
    //                typeDef.FullName,
    //                string.Join("|", genericArguments));
    //        }

    //        var dataContractAttribute = type.Module.Import(typeof(DataContractAttribute));
    //        var dataMemberAttribute = type.Module.Import(typeof(DataMemberAttribute));
    //        var items = typeDef
    //            .Properties
    //            .Where(p => p.GetMethod != null && p.GetMethod.IsPublic)
    //            .Where(p => !VersionMemberName.SupportedVersionPropertyNames.Contains(p.Name))
    //            .Where(p =>
    //                !p.DeclaringType.CustomAttributes
    //                    .Select(a => a.AttributeType)
    //                    .Any(t => t.IsProbablyEqualTo(dataContractAttribute))
    //                || p.CustomAttributes
    //                    .Select(a => a.AttributeType)
    //                    .Any(t => t.IsProbablyEqualTo(dataMemberAttribute)))
    //            .Select(p =>
    //            {
    //                TypeReference propertyType;
    //                if (p.PropertyType.IsGenericParameter)
    //                {
    //                    Debug.Assert(genericInstance != null);
    //                    var index = p.DeclaringType.GenericParameters.IndexOf(((GenericParameter)p.PropertyType));
    //                    propertyType = genericInstance.GenericArguments[index];
    //                }
    //                else
    //                {
    //                    propertyType = p.PropertyType;
    //                }
    //                return string.Format(
    //                    "{0}-{1}",
    //                    GenerateHashBaseInternal(propertyType, processedTypes), p.Name);
    //            })
    //            .OrderBy(p => p);

    //        return string.Format("{0}({1})", typeDef.FullName, string.Join("|", items));
    //    }

    //    private static readonly List<Type> SimpleTypes = new List<Type>
    //    {
    //        typeof(bool), typeof(byte), typeof(sbyte), typeof(char), typeof(decimal),
    //        typeof(double), typeof(float), typeof(int), typeof(uint), typeof(long),
    //        typeof(ulong), typeof(short), typeof(ushort), typeof(string)
    //    };

    //    private static bool IsSimpleType(ITypeSymbol type)
    //    {
    //        return SimpleTypes
    //            .Any(t => t.Name == type.Name);
    //    }

    //    private static bool IsEnumerable(TypeReference type)
    //    {
    //        return type.HasInterface(type.Module.Import(typeof(System.Collections.IEnumerable)).Resolve());
    //    }
    //}
}