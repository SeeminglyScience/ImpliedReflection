using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;

using AdapterSet = System.Management.Automation.PSObject.AdapterSet;

namespace ImpliedReflection
{
    internal static class PSObjectProxy
    {
#pragma warning disable IDE1006
        public static ref ConcurrentDictionary<Type, AdapterSet> s_adapterMapping
            => ref Reflect.StaticFieldRef<ConcurrentDictionary<Type, AdapterSet>>(
                typeof(PSObject),
                FieldNames.PSObject.s_adapterMapping);

        public static ref Collection<CollectionEntry<PSMemberInfo>> s_memberCollection
            => ref Reflect.StaticFieldRef<Collection<CollectionEntry<PSMemberInfo>>>(
                typeof(PSObject),
                FieldNames.PSObject.s_memberCollection);

        public static ref Collection<CollectionEntry<PSMethodInfo>> s_methodCollection
            => ref Reflect.StaticFieldRef<Collection<CollectionEntry<PSMethodInfo>>>(
                typeof(PSObject),
                FieldNames.PSObject.s_methodCollection);

        public static ref Collection<CollectionEntry<PSPropertyInfo>> s_propertyCollection
            => ref Reflect.StaticFieldRef<Collection<CollectionEntry<PSPropertyInfo>>>(
                typeof(PSObject),
                FieldNames.PSObject.s_propertyCollection);

        public static ref DotNetAdapter s_dotNetInstanceAdapter
            => ref Reflect.StaticFieldRef<DotNetAdapter>(
                typeof(PSObject),
                FieldNames.PSObject.s_dotNetInstanceAdapter);

        public static ref DotNetAdapter s_dotNetStaticAdapter
            => ref Reflect.StaticFieldRef<DotNetAdapter>(
                typeof(PSObject),
                FieldNames.PSObject.s_dotNetStaticAdapter);

        public static ref AdapterSet s_dotNetInstanceAdapterSet
            => ref Reflect.StaticFieldRef<AdapterSet>(
                typeof(PSObject),
                FieldNames.PSObject.s_dotNetInstanceAdapterSet);
#pragma warning restore IDE1006
    }
}
