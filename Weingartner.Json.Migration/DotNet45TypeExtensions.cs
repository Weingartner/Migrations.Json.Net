using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Weingartner.Json.Migration
{
    public static class DotNet45TypeExtensions
    {

        public static TAttributeType GetCustomAttribute<TAttributeType>(this Type type)
            where TAttributeType : Attribute
        {
            TAttributeType result = null;
            var attributes = type.GetCustomAttributes(typeof(TAttributeType), false);
            if (attributes.Length != 0)
            {
                result = (TAttributeType)attributes[0];
            }
            return result;
        }

        public static  MethodInfo GetDeclaredMethod(this Type @this, string name)
        {
            return @this.GetMethod(name, DeclaredOnlyLookup);
        }

        public static IEnumerable<MethodInfo> GetDeclaredMethods(this Type @this)
        {
            return @this.GetMethods(DeclaredOnlyLookup);
        }

        internal const BindingFlags DeclaredOnlyLookup = 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        public static Type[] GenericTypeArguments (this Type @this)
        {
            if (@this.IsGenericType && !@this.IsGenericTypeDefinition)
            {
                return @this.GetGenericArguments();
            }
            return new Type[0];
        }
    }
}