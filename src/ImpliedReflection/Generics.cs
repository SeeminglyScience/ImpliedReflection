using System;
using System.Management.Automation;
using System.Reflection;

using static ImpliedReflection.Cache;
using static ImpliedReflection.VersionSpecificMemberNames;

namespace ImpliedReflection
{
    internal static class Generics<TMemberType>
        where TMemberType : PSMemberInfo
    {
        internal static Type InternalCollection;

        internal static MethodInfo InternalCollection_Add;

        internal static Type GetMember;

        internal static Type GetMembers;

        internal static Type Entry;

        internal static FieldInfo Entry_GetMember;

        internal static FieldInfo Entry_GetMembers;

        static Generics()
        {
            InternalCollection = PSMemberInfoInternalCollection.MakeGenericType(typeof(TMemberType));
            InternalCollection_Add = InternalCollection.GetMethod(
                nameof(System.Collections.IList.Add),
                new[] { typeof(TMemberType), typeof(bool) });

            GetMember = GetMemberDelegate.MakeGenericType(typeof(TMemberType));
            GetMembers = GetMembersDelegate.MakeGenericType(typeof(TMemberType));
            Entry = CollectionEntry.MakeGenericType(typeof(TMemberType));
            Entry_GetMember = Entry.GetField(
                PrivateMemberNames.CollectionEntry_GetMember,
                Bind.NonPublic.Instance);
            Entry_GetMembers = Entry.GetField(
                PrivateMemberNames.CollectionEntry_GetMembers,
                Bind.NonPublic.Instance);
        }
    }
}
