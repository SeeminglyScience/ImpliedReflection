namespace ImpliedReflection
{
    internal static class FieldNames
    {
        internal static class Binders
        {
            public const string s_binderCache = "_binderCache";
            public const string s_binderCacheIgnoringCase = "_binderCacheIgnoringCase";
            public const string _version = "_version";
            public const string _constraints = "_constraints";
            public const string _callInfo = "_callInfo";
        }

        internal static class PSObject
        {
            public const string s_adapterMapping = "_adapterMapping";
            public const string s_memberCollection = "memberCollection";
            public const string s_methodCollection = "methodCollection";
            public const string s_propertyCollection = "propertyCollection";
            public const string s_dotNetInstanceAdapter = "dotNetInstanceAdapter";
            public const string s_dotNetStaticAdapter = "dotNetStaticAdapter";
            public const string s_dotNetInstanceAdapterSet = "dotNetInstanceAdapterSet";
        }

        internal static class DotNetAdapter
        {
            public const string s_instanceEventCacheTable = "instanceEventCacheTable";
            public const string s_staticEventCacheTable = "staticEventCacheTable";
            public const string s_instancePropertyCacheTable = "instancePropertyCacheTable";
            public const string s_staticPropertyCacheTable = "staticPropertyCacheTable";
            public const string s_instanceMethodCacheTable = "instanceMethodCacheTable";
            public const string s_staticMethodCacheTable = "staticMethodCacheTable";
        }
    }
}
