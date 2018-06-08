using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SageConnector.Logic
{
    public static class EnumerableUtils
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }

        public static Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action)
        {
            return Task.WhenAll(enumerable.Select(action));
        }
    }
}