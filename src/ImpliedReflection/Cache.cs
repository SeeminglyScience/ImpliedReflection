using System;
using System.Management.Automation;
using System.Reflection;

using static ImpliedReflection.VersionSpecificMemberNames;

namespace ImpliedReflection
{
    internal static class Cache
    {
        public static ConstructorInfo RuntimeException_ctor = typeof(RuntimeException).
            GetConstructor(
                Bind.Public.Instance,
                binder: null,
                new[] { typeof(string) },
                modifiers: null);

        public static MethodInfo PropertyInfo_GetGetMethod = typeof(PropertyInfo)
            .GetMethod(
                nameof(PropertyInfo.GetGetMethod),
                Bind.Public.Instance,
                null,
                new[] { typeof(bool) },
                null);

        public static MethodInfo PropertyInfo_GetSetMethod = typeof(PropertyInfo)
            .GetMethod(
                nameof(PropertyInfo.GetSetMethod),
                Bind.Public.Instance,
                null,
                new[] { typeof(bool) },
                null);

        public static ConstructorInfo GeneratedProxyHelpersAttribute_ctor = typeof(Internal.GeneratedProxyHelpersAttribute)
            .GetConstructor(
                Bind.Public.Instance,
                null,
                Type.EmptyTypes,
                null);

        public static MethodInfo string_Equals = typeof(string).GetMethod(
            nameof(string.Equals),
            Bind.Public.Instance,
            null,
            new[] { typeof(string), typeof(StringComparison) },
            new[] { new ParameterModifier(2) });

        public static MethodInfo Object_GetType = typeof(object).GetMethod(nameof(object.GetType));

        public static PropertyInfo PSObject_BaseObject = typeof(PSObject).GetProperty(nameof(PSObject.BaseObject));

        public static Type GetMembersDelegate;

        public static Type GetMemberDelegate;

        public static Type PSMemberInfoInternalCollection;

        public static Type PropertyCacheEntry;

        public static ConstructorInfo PropertyCacheEntry_ctor_PropertyInfo;

        public static ConstructorInfo PropertyCacheEntry_ctor_FieldInfo;

        public static FieldInfo PropertyCacheEntry_writeOnly;

        public static FieldInfo PropertyCacheEntry_readOnly;

        public static Type MethodCacheEntry;

        public static ConstructorInfo MethodCacheEntry_ctor;

        public static FieldInfo MethodCacheEntry_methodInformationStructures;

        public static Type Adapter;

        public static ConstructorInfo PSProperty_ctor;

        public static ConstructorInfo PSMethod_ctor;

        public static FieldInfo PSObject_dotNetInstanceAdapter;

        public static Type CollectionEntry;

        public static Type DotNetAdapter;

        public static FieldInfo DotNetAdapter_s_disallowReflectionCache;

        public static FieldInfo DotNetAdapter_instancePropertyCacheTable;

        public static FieldInfo DotNetAdapter_staticPropertyCacheTable;

        public static FieldInfo DotNetAdapter_instanceMethodCacheTable;

        public static FieldInfo DotNetAdapter_staticEventCacheTable;

        public static FieldInfo DotNetAdapter_instanceEventCacheTable;

        public static FieldInfo DotNetAdapter_staticMethodCacheTable;

        public static Type CacheTable;

        public static MethodInfo CacheTable_Add;

        public static FieldInfo CacheTable_indexes;

        public static FieldInfo CacheTable_memberCollection;

        public static Type MethodInformation;

        public static FieldInfo MethodInformation_method;

        public static Type IsByRefLikeAttribute = typeof(object).Assembly.GetType(
            "System.Runtime.CompilerServices.IsByRefLikeAttribute");

        static Cache()
        {
            GetMemberDelegate = typeof(PSObject).Assembly
                .GetType(PrivateMemberNames.GetMemberDelegate);

            GetMembersDelegate = typeof(PSObject).Assembly
                .GetType(PrivateMemberNames.GetMembersDelegate);

            PSMemberInfoInternalCollection = typeof(PSObject).Assembly
                .GetType(PrivateMemberNames.PSMemberInfoInternalCollection);

            PropertyCacheEntry = typeof(PSObject).Assembly.GetType(PrivateMemberNames.PropertyCacheEntry);
            PropertyCacheEntry_ctor_PropertyInfo = PropertyCacheEntry
                .GetConstructor(
                    Bind.NonPublic.Instance,
                    null,
                    new[] { typeof(PropertyInfo) },
                    new[] { new ParameterModifier(1) });

            PropertyCacheEntry_ctor_FieldInfo = PropertyCacheEntry
                .GetConstructor(
                    Bind.NonPublic.Instance,
                    null,
                    new[] { typeof(FieldInfo) },
                    new[] { new ParameterModifier(1) });

            PropertyCacheEntry_readOnly = PropertyCacheEntry.GetField(
                PrivateMemberNames.PropertyCacheEntry_readOnly,
                Bind.NonPublic.Instance);

            PropertyCacheEntry_writeOnly = PropertyCacheEntry.GetField(
                PrivateMemberNames.PropertyCacheEntry_writeOnly,
                Bind.NonPublic.Instance);

            MethodCacheEntry = typeof(PSObject).Assembly.GetType(PrivateMemberNames.MethodCacheEntry);
            MethodCacheEntry_ctor = MethodCacheEntry.GetConstructor(
                Bind.NonPublic.Instance,
                null,
                new[] { typeof(MethodBase[]) },
                new[] { new ParameterModifier(1) });

            MethodCacheEntry_methodInformationStructures = MethodCacheEntry.GetField(
                PrivateMemberNames.MethodCacheEntry_methodInformationStructures,
                Bind.NonPublic.Instance);

            Adapter = typeof(PSObject).Assembly.GetType(PrivateMemberNames.Adapter);

            PSProperty_ctor = typeof(PSProperty).GetConstructor(
                    Bind.NonPublic.Instance,
                    null,
                    new[] { typeof(string), Adapter, typeof(object), typeof(object) },
                    new[] { new ParameterModifier(4) });

            PSMethod_ctor = typeof(PSMethod).GetConstructor(
                    Bind.NonPublic.Instance,
                    null,
                    new[] { typeof(string), Adapter, typeof(object), typeof(object), typeof(bool), typeof(bool) },
                    new[] { new ParameterModifier(6) });

            PSObject_dotNetInstanceAdapter = typeof(PSObject)
                .GetField(
                    PrivateMemberNames.PSObject_DotNetInstanceAdapter,
                    Bind.NonPublic.Static);

            CollectionEntry = typeof(PSObject).Assembly.GetType(PrivateMemberNames.CollectionEntry);

            DotNetAdapter = typeof(PSObject).Assembly.GetType(PrivateMemberNames.DotNetAdapter);

            DotNetAdapter_s_disallowReflectionCache = DotNetAdapter.GetField(
                "s_disallowReflectionCache",
                Bind.NonPublic.Static);

            DotNetAdapter_instancePropertyCacheTable =
                DotNetAdapter.GetField(
                    PrivateMemberNames.DotNetAdapter_instancePropertyCacheTable,
                    Bind.NonPublic.Static);

            DotNetAdapter_staticPropertyCacheTable =
                DotNetAdapter.GetField(
                    PrivateMemberNames.DotNetAdapter_staticPropertyCacheTable,
                    Bind.NonPublic.Static);

            DotNetAdapter_instanceMethodCacheTable =
                DotNetAdapter.GetField(
                    PrivateMemberNames.DotNetAdapter_instanceMethodCacheTable,
                    Bind.NonPublic.Static);

            DotNetAdapter_staticEventCacheTable =
                DotNetAdapter.GetField(
                    PrivateMemberNames.DotNetAdapter_staticEventCacheTable,
                    Bind.NonPublic.Static);

            DotNetAdapter_instanceEventCacheTable =
                DotNetAdapter.GetField(
                    PrivateMemberNames.DotNetAdapter_instanceEventCacheTable,
                    Bind.NonPublic.Static);

            DotNetAdapter_staticMethodCacheTable =
                DotNetAdapter.GetField(
                    PrivateMemberNames.DotNetAdapter_staticMethodCacheTable,
                    Bind.NonPublic.Static);

            CacheTable = typeof(PSObject).Assembly.GetType(PrivateMemberNames.CacheTable);

            CacheTable_Add = CacheTable.GetMethod(
                PrivateMemberNames.CacheTable_Add,
                Bind.NonPublic.Instance,
                null,
                new[] { typeof(string), typeof(object) },
                new[] { new ParameterModifier(2) });

            CacheTable_indexes = CacheTable.GetField(
                PrivateMemberNames.CacheTable_indexes,
                Bind.NonPublic.Instance);

            CacheTable_memberCollection = CacheTable.GetField(
                PrivateMemberNames.CacheTable_memberCollection,
                Bind.NonPublic.Instance);

            MethodInformation = typeof(PSObject).Assembly.GetType(PrivateMemberNames.MethodInformation);

            MethodInformation_method = MethodInformation.GetField(
                PrivateMemberNames.MethodInformation_method,
                Bind.NonPublic.Instance);
        }
    }
}
