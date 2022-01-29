using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;

namespace ImpliedReflection
{
    // Item1 - member name
    // Item2 - class containing dynamic site (for protected/private member access)
    // Item3 - static (true) or instance (false)
    // Item4 - enumerating (true) or not (false)
    using PSGetMemberBinderKeyType = Tuple<string, Type, bool, bool>;

    // Item1 - member name
    // Item2 - class containing dynamic site (for protected/private member access)
    // Item3 - enumerating (true) or not (false)
    using PSSetMemberBinderKeyType = Tuple<string, Type, bool>;

    // Item1 - member name
    // Item2 - callinfo (# of args and (not used) named arguments)
    // Item3 - property setter (true) or not (false)
    // Item4 - enumerating (true) or not (false)
    // Item5 - invocation constraints (casts used in the invocation expression used to guide overload resolution)
    // Item6 - static (true) or instance (false)
    // Item7 - class containing dynamic site (for protected/private member access)
    using PSInvokeMemberBinderKeyType = Tuple<string, CallInfo, bool, bool, PSMethodInvocationConstraints, bool, Type>;

    // Item1 - callinfo (# of args and (not used) named arguments)
    // Item2 - invocation constraints (casts used in the invocation expression used to guide overload resolution)
    // Item3 - property setter (true) or not (false)
    // Item4 - static (true) or instance (false)
    // Item5 - class containing dynamic site (for protected/private member access)
    using PSInvokeDynamicMemberBinderKeyType = Tuple<CallInfo, PSMethodInvocationConstraints, bool, bool, Type>;

    // Item1 - class containing dynamic site (for protected/private member access)
    // Item2 - static (true) or instance (false)
    using PSGetOrSetDynamicMemberBinderKeyType = Tuple<Type, bool>;

    // Disable naming convention warnings. These properties mirror private fields.
#pragma warning disable IDE1006
    internal static class Binders
    {
        public static class GetMember
        {
            public static ref Dictionary<PSGetMemberBinderKeyType, PSGetMemberBinder> s_binderCache
                => ref Reflect.StaticFieldRef<Dictionary<PSGetMemberBinderKeyType, PSGetMemberBinder>>(
                    typeof(PSGetMemberBinder),
                    FieldNames.Binders.s_binderCache);

            public static ref ConcurrentDictionary<string, List<PSGetMemberBinder>> s_binderCacheIgnoringCase
                => ref Reflect.StaticFieldRef<ConcurrentDictionary<string, List<PSGetMemberBinder>>>(
                    typeof(PSGetMemberBinder),
                    FieldNames.Binders.s_binderCacheIgnoringCase);
        }

        public static class InvokeDynamicMember
        {
            public static ref Dictionary<PSInvokeDynamicMemberBinderKeyType, PSInvokeDynamicMemberBinder> s_binderCache
                => ref Reflect.StaticFieldRef<Dictionary<PSInvokeDynamicMemberBinderKeyType, PSInvokeDynamicMemberBinder>>(
                    typeof(PSInvokeDynamicMemberBinder),
                    FieldNames.Binders.s_binderCache);
        }

        public static class GetDynamicMember
        {
            public static ref Dictionary<PSGetOrSetDynamicMemberBinderKeyType, PSGetDynamicMemberBinder> s_binderCache
                => ref Reflect.StaticFieldRef<Dictionary<PSGetOrSetDynamicMemberBinderKeyType, PSGetDynamicMemberBinder>>(
                    typeof(PSGetDynamicMemberBinder),
                    FieldNames.Binders.s_binderCache);
        }

        public static class SetDynamicMember
        {
            public static ref Dictionary<PSGetOrSetDynamicMemberBinderKeyType, PSSetDynamicMemberBinder> s_binderCache
                => ref Reflect.StaticFieldRef<Dictionary<PSGetOrSetDynamicMemberBinderKeyType, PSSetDynamicMemberBinder>>(
                    typeof(PSSetDynamicMemberBinder),
                    FieldNames.Binders.s_binderCache);
        }

        public static class SetMember
        {
            public static ref Dictionary<PSSetMemberBinderKeyType, PSSetMemberBinder> s_binderCache
                => ref Reflect.StaticFieldRef<Dictionary<PSSetMemberBinderKeyType, PSSetMemberBinder>>(
                    typeof(PSSetMemberBinder),
                    FieldNames.Binders.s_binderCache);
        }

        public static class InvokeMember
        {
            public static ref Dictionary<PSInvokeMemberBinderKeyType, PSInvokeMemberBinder> s_binderCache
                => ref Reflect.StaticFieldRef<Dictionary<PSInvokeMemberBinderKeyType, PSInvokeMemberBinder>>(
                    typeof(PSInvokeMemberBinder),
                    FieldNames.Binders.s_binderCache);
        }

        public static class CreateInstance
        {
            public static ref Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints, bool>, PSCreateInstanceBinder> s_binderCache
                => ref Reflect.StaticFieldRef<Dictionary<Tuple<CallInfo, PSMethodInvocationConstraints, bool>, PSCreateInstanceBinder>>(
                    typeof(PSInvokeMemberBinder),
                    FieldNames.Binders.s_binderCache);
        }

        // So many things wrong here that I took the lazy way to fix. Don't do this.
        public static unsafe ref int _version(this PSCreateInstanceBinder self)
        {
            ref int @ref = ref Reflect.FieldRef<PSCreateInstanceBinder, int>(
                ref self,
                FieldNames.Binders._version);

            return ref *(int*)Unsafe.AsPointer(ref @ref);
        }

        public static unsafe ref PSMethodInvocationConstraints _constraints(this PSCreateInstanceBinder self)
        {
            return ref Reflect.FieldRef<PSCreateInstanceBinder, PSMethodInvocationConstraints>(
                ref Unsafe.AsRef<PSCreateInstanceBinder>(Unsafe.AsPointer(ref self)),
                FieldNames.Binders._constraints);
        }

        public static unsafe ref CallInfo _callInfo(this PSCreateInstanceBinder self)
        {
            return ref Reflect.FieldRef<PSCreateInstanceBinder, CallInfo>(
                ref Unsafe.AsRef<PSCreateInstanceBinder>(Unsafe.AsPointer(ref self)),
                FieldNames.Binders._callInfo);
        }
    }
}

#pragma warning restore IDE1006
