using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Weingartner.Json.Migration.Fody
{
    public class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference>
    {
        public static TypeReferenceEqualityComparer Default = new TypeReferenceEqualityComparer();

        public bool Equals(TypeReference x, TypeReference y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x == null || y == null)
                return false;
            return x.FullName == y.FullName && x.Scope.MetadataToken == y.Scope.MetadataToken;
        }

        public int GetHashCode(TypeReference obj)
        {
            throw new NotImplementedException();
        }
    }
}