namespace ImpliedReflection
{
    internal class CoreCLRNames : VersionSpecificMemberNames
    {
        public override string PSObject_memberCollection => "s_memberCollection";

        public override string PSObject_methodCollection => "s_methodCollection";

        public override string PSObject_propertyCollection => "s_propertyCollection";

        public override string CollectionEntry_GetMember => "<GetMember>k__BackingField";

        public override string CollectionEntry_GetMembers => "<GetMembers>k__BackingField";

        public override string DotNetAdapter_instancePropertyCacheTable => "s_instancePropertyCacheTable";

        public override string DotNetAdapter_staticPropertyCacheTable => "s_staticPropertyCacheTable";

        public override string DotNetAdapter_instanceMethodCacheTable => "s_instanceMethodCacheTable";

        public override string DotNetAdapter_staticMethodCacheTable => "s_staticMethodCacheTable";

        public override string DotNetAdapter_staticEventCacheTable => "s_staticEventCacheTable";

        public override string DotNetAdapter_instanceEventCacheTable => "s_instanceEventCacheTable";

        public override string CacheTable_indexes => "_indexes";
    }
}
