using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

using static ImpliedReflection.VersionSpecificMemberNames;

namespace ImpliedReflection
{
    internal class DelegateController
    {
        private static readonly HashSet<Runspace> s_boundRunspaces = new HashSet<Runspace>();

        private static bool s_areDelegatesOverriden;

        private static object s_syncObject = new object();

        public static bool Enable()
        {
            lock (s_syncObject)
            {
                if (!s_areDelegatesOverriden)
                {
                    BindGetters<PSMemberInfo>();
                    BindGetters<PSPropertyInfo>();
                    BindGetters<PSMethodInfo>();
                    s_areDelegatesOverriden = true;
                }

                return s_boundRunspaces.Add(Runspace.DefaultRunspace);
            }
        }

        public static bool Disable()
        {
            lock (s_syncObject)
            {
                if (!s_boundRunspaces.Contains(Runspace.DefaultRunspace))
                {
                    if (s_boundRunspaces.Count == 0)
                    {
                        return false;
                    }

                    if (s_boundRunspaces.All(IsRunspaceClosed))
                    {
                        s_boundRunspaces.Clear();
                    }
                }

                if (!s_areDelegatesOverriden)
                {
                    return true;
                }

                UnbindGetters<PSMemberInfo>();
                UnbindGetters<PSPropertyInfo>();
                UnbindGetters<PSMethodInfo>();
                s_areDelegatesOverriden = false;
            }

            return true;
        }

        private static bool IsRunspaceClosed(Runspace runspace)
        {
            RunspaceState state = runspace.RunspaceStateInfo.State;
            return state == RunspaceState.Broken
                || state == RunspaceState.Closed
                || state == RunspaceState.Closing;
        }

        private static object GetClrCollectionEntry<TMemberType>()
            where TMemberType : PSMemberInfo
        {
            string memberCollectionName = PrivateMemberNames.PSObject_memberCollection;
            Type memberType = typeof(PSMemberInfo);
            if (typeof(TMemberType) == typeof(PSPropertyInfo))
            {
                memberCollectionName = PrivateMemberNames.PSObject_propertyCollection;
                memberType = typeof(PSPropertyInfo);
            }

            if (typeof(TMemberType) == typeof(PSMethodInfo))
            {
                memberCollectionName = PrivateMemberNames.PSObject_methodCollection;
                memberType = typeof(PSMethodInfo);
            }

            object memberCollection = typeof(PSObject)
                .GetField(memberCollectionName, Bind.NonPublic.Static)
                .GetValue(null);

            PropertyInfo collectionNameProperty = Generics<TMemberType>.Entry
                .GetProperty(
                    PrivateMemberNames.CollectionEntry_CollectionNameForTracing,
                    Bind.NonPublic.Instance);

            IEnumerator colEnumerator = (IEnumerator)typeof(Collection<>)
                .MakeGenericType(Generics<TMemberType>.Entry)
                .GetMethod(nameof(CollectionBase.GetEnumerator))
                .Invoke(memberCollection, null);

            try
            {
                while (colEnumerator.MoveNext())
                {
                    if (((string)collectionNameProperty.GetValue(colEnumerator.Current))
                        .Equals(StringLiterals.ClrMembersCollectionName, StringComparison.OrdinalIgnoreCase))
                    {
                        return colEnumerator.Current;
                    }
                }
            }
            finally
            {
                (colEnumerator as IDisposable)?.Dispose();
            }

            return null;
        }

        private static void UnbindGetters<TMemberType>()
            where TMemberType : PSMemberInfo
        {
            object clrMembersEntry = GetClrCollectionEntry<TMemberType>();
            Generics<TMemberType>.Entry_GetMember.SetValue(
                clrMembersEntry,
                Originals<TMemberType>.GetMember);

            Generics<TMemberType>.Entry_GetMembers.SetValue(
                clrMembersEntry,
                Originals<TMemberType>.GetMembers);
        }

        private static void BindGetters<TMemberType>()
            where TMemberType : PSMemberInfo
        {
            object clrMembersEntry = GetClrCollectionEntry<TMemberType>();
            Originals<TMemberType>.GetMember =
                (MulticastDelegate)Generics<TMemberType>.Entry_GetMember.GetValue(clrMembersEntry);

            Originals<TMemberType>.GetMembers =
                (MulticastDelegate)Generics<TMemberType>.Entry_GetMembers.GetValue(clrMembersEntry);

            Generics<TMemberType>.Entry_GetMember.SetValue(
                clrMembersEntry,
                Overrides<TMemberType>.GetMember);

            Generics<TMemberType>.Entry_GetMembers.SetValue(
                clrMembersEntry,
                Overrides<TMemberType>.GetMembers);
        }
    }
}
