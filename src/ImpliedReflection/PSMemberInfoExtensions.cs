using System;
using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace ImpliedReflection
{
    internal static class PSMemberInfoExtensions
    {
        private static readonly Action<PSMemberInfo, object> s_replicateInstance;

        public static readonly CallSite<Action<CallSite, PSMemberInfo, object>> s_callsite
            = CallSite<Action<CallSite, PSMemberInfo, object>>.Create(ReplicateInstanceBinder.Instance);

        public static TMemberInfo Clone<TMemberInfo>(this TMemberInfo member)
            where TMemberInfo : PSMemberInfo
        {
            return (TMemberInfo)member.Copy();
        }

        public static void ReplicateInstance(this PSMemberInfo member, object instance)
            => s_callsite.Update(s_callsite, member, instance);
    }
}
