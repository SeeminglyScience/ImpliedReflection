using System;
using System.Management.Automation;

namespace ImpliedReflection
{
    internal static class Originals<TMemberType>
        where TMemberType : PSMemberInfo
    {
        private static object s_syncLock = new object();

        private static MulticastDelegate s_getMember;

        private static MulticastDelegate s_getMembers;

        public static MulticastDelegate GetMember
        {
            get
            {
                lock (s_syncLock)
                {
                    return s_getMember;
                }
            }
            set
            {
                lock (s_syncLock)
                {
                    if (s_getMember != null && value != null)
                    {
                        return;
                    }

                    s_getMember = value;
                }
            }
        }

        public static MulticastDelegate GetMembers
        {
            get
            {
                lock (s_syncLock)
                {
                    return s_getMembers;
                }
            }
            set
            {
                lock (s_syncLock)
                {
                    if (s_getMembers != null && value != null)
                    {
                        return;
                    }

                    s_getMembers = value;
                }
            }
        }
    }
}
