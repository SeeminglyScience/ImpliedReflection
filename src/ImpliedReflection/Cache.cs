using System;

namespace ImpliedReflection
{
    internal static class Cache
    {
        public static Type IsByRefLikeAttribute = typeof(object).Assembly.GetType(
            "System.Runtime.CompilerServices.IsByRefLikeAttribute");
    }
}
