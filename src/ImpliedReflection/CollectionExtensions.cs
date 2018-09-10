using System.Collections.Generic;

namespace ImpliedReflection
{
    internal static class CollectionExtensions
    {
        internal static int SequenceGetHashCode<T>(this IEnumerable<T> xs)
        {
            // algorithm based on http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
            if (xs == null)
            {
                return 82460653; // random number
            }
            unchecked
            {
                int hash = 41; // 41 is a random prime number
                foreach (T x in xs)
                {
                    hash = hash * 59; // 59 is a random prime number
                    if (x != null)
                    {
                        hash = hash + x.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }
}
