using System;
using System.Collections.Generic;
using System.Management.Automation;
using static System.Management.Automation.DotNetAdapter;

namespace ImpliedReflection
{
    internal static class DotNetAdapterProxy
    {
#pragma warning disable IDE1006
        public static ref Dictionary<Type, Dictionary<string, EventCacheEntry>> s_instanceEventCacheTable
            => ref Reflect.StaticFieldRef<Dictionary<Type, Dictionary<string, EventCacheEntry>>>(
                typeof(DotNetAdapter),
                FieldNames.DotNetAdapter.s_instanceEventCacheTable);

        public static ref Dictionary<Type, Dictionary<string, EventCacheEntry>> s_staticEventCacheTable
            => ref Reflect.StaticFieldRef<Dictionary<Type, Dictionary<string, EventCacheEntry>>>(
                typeof(DotNetAdapter),
                FieldNames.DotNetAdapter.s_staticEventCacheTable);

        public static ref Dictionary<Type, CacheTable> s_instancePropertyCacheTable
            => ref Reflect.StaticFieldRef<Dictionary<Type, CacheTable>>(
                typeof(DotNetAdapter),
                FieldNames.DotNetAdapter.s_instancePropertyCacheTable);

        public static ref Dictionary<Type, CacheTable> s_staticPropertyCacheTable
            => ref Reflect.StaticFieldRef<Dictionary<Type, CacheTable>>(
                typeof(DotNetAdapter),
                FieldNames.DotNetAdapter.s_staticPropertyCacheTable);

        public static ref Dictionary<Type, CacheTable> s_instanceMethodCacheTable
            => ref Reflect.StaticFieldRef<Dictionary<Type, CacheTable>>(
                typeof(DotNetAdapter),
                FieldNames.DotNetAdapter.s_instanceMethodCacheTable);

        public static ref Dictionary<Type, CacheTable> s_staticMethodCacheTable
            => ref Reflect.StaticFieldRef<Dictionary<Type, CacheTable>>(
                typeof(DotNetAdapter),
                FieldNames.DotNetAdapter.s_staticMethodCacheTable);
#pragma warning restore IDE1006
    }
}
