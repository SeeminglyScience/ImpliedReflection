using System;
using System.Management.Automation;

using static System.Management.Automation.DotNetAdapter;

namespace ImpliedReflection
{
    internal static partial class Polyfill
    {
        public static PSMethod CreatePSMethod(string name, DotNetAdapter dotNetInstanceAdapter, object baseObject, MethodCacheEntry method, bool isSpecial, bool isHidden)
        {
            return PSMethod.Create(
                name,
                dotNetInstanceAdapter,
                baseObject,
                method,
                isSpecial,
                isHidden);
        }

        public static PSTraceSource MemberResolution => PSObject.MemberResolution;
    }

    internal static partial class PolyfillExtensions
    {
        public static bool GetIsHidden(this MethodCacheEntry self)
        {
            return self.IsHidden;
        }

        public static bool GetIsHidden(this PropertyCacheEntry self)
        {
            return self.IsHidden;
        }

        public static bool GetIsByRefLike(this Type self)
        {
            return self.IsByRefLike;
        }
    }
}
