using System;
using System.Collections.Generic;
using System.Linq;

namespace Eleven19.Net.Http.Formatting.Internal
{
    internal static class TypeExtensions
    {
        public static IEnumerable<Type> GetTypesThatClose(this Type @this, Type openGeneric)
        {
            return FindAssignableTypesThatClose(@this, openGeneric);
        }

        public static bool IsClosedTypeOf(this Type @this, Type openGeneric)
        {
            if (@this == null)
            {
                throw new ArgumentNullException("this");
            }
            if (openGeneric == null)
            {
                throw new ArgumentNullException("openGeneric");
            }                

            return TypesAssignableFrom(@this).Any(t =>
            {
                if (t.IsGenericType && !@this.ContainsGenericParameters)
                {
                    return t.GetGenericTypeDefinition() == openGeneric;
                }                    
                return false;
            });
        }

        public static bool TryGetElementType(this Type @this, out Type elementType)
        {
            if (!@this.IsClosedTypeOf(typeof (IEnumerable<>)))
            {
                elementType = null;
                return false;
            }
            elementType = @this.GetGenericArguments()[0];
            return true;
        }

        private static IEnumerable<Type> FindAssignableTypesThatClose(Type candidateType, Type openGenericServiceType)
        {
            return TypesAssignableFrom(candidateType).Where(t => IsClosedTypeOf(t, openGenericServiceType));
        }

        private static IEnumerable<Type> TypesAssignableFrom(Type candidateType)
        {
            return candidateType
                .GetInterfaces()
                .Concat(Traverse.Across(candidateType, t => t.BaseType));
        }
    }
}
