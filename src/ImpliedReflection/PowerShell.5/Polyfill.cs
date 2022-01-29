using System;
using System.Management.Automation;

using static System.Management.Automation.DotNetAdapter;

namespace ImpliedReflection
{
    internal static partial class Polyfill
    {
        public static PSMethod CreatePSMethod(string name, DotNetAdapter dotNetInstanceAdapter, object baseObject, MethodCacheEntry method, bool isSpecial, bool isHidden)
        {
            return new PSMethod(
                name,
                dotNetInstanceAdapter,
                baseObject,
                method,
                isSpecial,
                isHidden);
        }

        public static PSTraceSource MemberResolution => PSObject.memberResolution;
    }

    internal static partial class PolyfillExtensions
    {
        public static bool GetIsHidden(this MethodCacheEntry self)
        {
            return false;
        }

        public static bool GetIsHidden(this PropertyCacheEntry self)
        {
            return false;
        }

        public static bool GetIsByRefLike(this Type self)
        {
            return Cache.IsByRefLikeAttribute != null &&
                self.IsDefined(Cache.IsByRefLikeAttribute, inherit: false);
        }
    }
}
