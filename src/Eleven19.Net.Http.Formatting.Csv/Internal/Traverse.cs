using System;
using System.Collections.Generic;

namespace Eleven19.Net.Http.Formatting.Internal
{
    internal static class Traverse
    {
        public static IEnumerable<T> Across<T>(T first, Func<T, T> next) where T : class
        {
            for (T item = first; (object)item != null; item = next(item))
                yield return item;
        }
    }
}