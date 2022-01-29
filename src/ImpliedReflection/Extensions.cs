using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ImpliedReflection;

internal static class Extensions
{
    public static MethodInfo Prepare(this MethodInfo method)
    {
        RuntimeHelpers.PrepareMethod(method.MethodHandle);
        return method;
    }

    // From SMA.EnumerableExtensions
    internal static IEnumerable<T> Prepend<T>(this IEnumerable<T> collection, T element)
    {
        yield return element;
        foreach (T t in collection)
            yield return t;
    }
}
