using System.Management.Automation;

namespace ImpliedReflection
{
    internal abstract class VersionSpecificMemberNames
    {
        public static VersionSpecificMemberNames PrivateMemberNames;

        static VersionSpecificMemberNames()
        {
            var coreNames = new CoreCLRNames();
            if (typeof(PSObject).GetField(coreNames.PSObject_memberCollection, Bind.NonPublic.Static) != null)
            {
                PrivateMemberNames = coreNames;
                return;
            }

            PrivateMemberNames = new FullCLRNames();
        }

        /// <summary>
        /// Gets a string similar to "memberCollection".
        /// </summary>
        public abstract string PSObject_memberCollection { get; }

        /// <summary>
        /// Gets a string similar to "methodCollection".
        /// </summary>
        public abstract string PSObject_methodCollection { get; }

        /// <summary>
        /// Gets a string similar to "propertyCollection".
        /// </summary>
        public abstract string PSObject_propertyCollection { get; }

        /// <summary>
        /// Gets a string similar to "getMember".
        /// </summary>
        public abstract string CollectionEntry_GetMember { get; }

        /// <summary>
        /// Gets a string similar to "getMembers".
        /// </summary>
        public abstract string CollectionEntry_GetMembers { get; }

        /// <summary>
        /// Gets a string similar to "instancePropertyCacheTable".
        /// </summary>
        public abstract string DotNetAdapter_instancePropertyCacheTable { get; }

        /// <summary>
        /// Gets a string similar to "staticPropertyCacheTable".
        /// </summary>
        public abstract string DotNetAdapter_staticPropertyCacheTable { get; }

        /// <summary>
        /// Gets a string similar to "instanceMethodCacheTable".
        /// </summary>
        public abstract string DotNetAdapter_instanceMethodCacheTable { get; }

        /// <summary>
        /// Gets a string similar to "staticMethodCacheTable".
        /// </summary>
        public abstract string DotNetAdapter_staticMethodCacheTable { get; }

        /// <summary>
        /// Gets a string similar to "staticEventCacheTable".
        /// </summary>
        public abstract string DotNetAdapter_staticEventCacheTable { get; }

        /// <summary>
        /// Gets a string similar to "instanceEventCacheTable".
        /// </summary>
        public abstract string DotNetAdapter_instanceEventCacheTable { get; }

        /// <summary>
        /// Gets a string similar to "indexes".
        /// </summary>
        public abstract string CacheTable_indexes { get; }

        /// <summary>
        /// Gets a string similar to "dotNetInstanceAdapter".
        /// </summary>
        public virtual string PSObject_DotNetInstanceAdapter => "dotNetInstanceAdapter";

        /// <summary>
        /// Gets a string similar to "dotNetStaticAdapter".
        /// </summary>
        public virtual string PSObject_DotNetStaticAdapter => "dotNetStaticAdapter";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.CollectionEntry`1+GetMemberDelegate".
        /// </summary>
        public virtual string GetMemberDelegate => "System.Management.Automation.CollectionEntry`1+GetMemberDelegate";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.CollectionEntry`1+GetMembersDelegate".
        /// </summary>
        public virtual string GetMembersDelegate => "System.Management.Automation.CollectionEntry`1+GetMembersDelegate";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.PSMemberInfoInternalCollection`1".
        /// </summary>
        public virtual string PSMemberInfoInternalCollection => "System.Management.Automation.PSMemberInfoInternalCollection`1";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.Adapter".
        /// </summary>
        public virtual string Adapter => "System.Management.Automation.Adapter";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.DotNetAdapter".
        /// </summary>
        public virtual string DotNetAdapter => "System.Management.Automation.DotNetAdapter";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.CollectionEntry`1".
        /// </summary>
        public virtual string CollectionEntry => "System.Management.Automation.CollectionEntry`1";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.DotNetAdapter+PropertyCacheEntry".
        /// </summary>
        public virtual string PropertyCacheEntry => "System.Management.Automation.DotNetAdapter+PropertyCacheEntry";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.DotNetAdapter+MethodCacheEntry".
        /// </summary>
        public virtual string MethodCacheEntry => "System.Management.Automation.DotNetAdapter+MethodCacheEntry";

        /// <summary>
        /// Gets a string similar to "methodInformationStructures".
        /// </summary>
        public virtual string MethodCacheEntry_methodInformationStructures => "methodInformationStructures";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.CacheTable".
        /// </summary>
        public virtual string CacheTable => "System.Management.Automation.CacheTable";

        /// <summary>
        /// Gets a string similar to "writeOnly".
        /// </summary>
        public virtual string PropertyCacheEntry_writeOnly => "writeOnly";

        /// <summary>
        /// Gets a string similar to "readOnly".
        /// </summary>
        public virtual string PropertyCacheEntry_readOnly => "readOnly";

        /// <summary>
        /// Gets a string similar to "Add".
        /// </summary>
        public virtual string CacheTable_Add => "Add";

        /// <summary>
        /// Gets a string similar to "memberCollection".
        /// </summary>
        public virtual string CacheTable_memberCollection => "memberCollection";

        /// <summary>
        /// Gets a string similar to "System.Management.Automation.MethodInformation".
        /// </summary>
        public virtual string MethodInformation => "System.Management.Automation.MethodInformation";

        /// <summary>
        /// Gets a string similar to "method".
        /// </summary>
        public virtual string MethodInformation_method => "method";

        /// <summary>
        /// Gets a string similar to "CollectionNameForTracing".
        /// </summary>
        public virtual string CollectionEntry_CollectionNameForTracing => "CollectionNameForTracing";
    }
}
