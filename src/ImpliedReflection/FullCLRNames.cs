namespace ImpliedReflection
{
    internal class FullCLRNames : VersionSpecificMemberNames
    {
        public override string PSObject_memberCollection => "memberCollection";

        public override string PSObject_methodCollection => "methodCollection";

        public override string PSObject_propertyCollection => "propertyCollection";

        public override string CollectionEntry_GetMember => "getMember";

        public override string CollectionEntry_GetMembers => "getMembers";

        public override string DotNetAdapter_instancePropertyCacheTable => "instancePropertyCacheTable";

        public override string DotNetAdapter_staticPropertyCacheTable => "staticPropertyCacheTable";

        public override string DotNetAdapter_instanceMethodCacheTable => "instanceMethodCacheTable";

        public override string DotNetAdapter_staticMethodCacheTable => "staticMethodCacheTable";

        public override string DotNetAdapter_staticEventCacheTable => "staticEventCacheTable";

        public override string DotNetAdapter_instanceEventCacheTable => "instanceEventCacheTable";

        public override string CacheTable_indexes => "indexes";
    }
}
