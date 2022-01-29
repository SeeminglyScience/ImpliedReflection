using System;
using System.Management.Automation;
using System.Reflection;

namespace ImpliedReflection
{
    public partial class NonPublicAdapter : DotNetAdapter
    {
        private static CollectionEntry<T> CreateCollectionEntry<T>(
            CollectionEntry<T>.GetMembersDelegate getMembers,
            CollectionEntry<T>.GetMemberDelegate getMember,
            Func<PSObject, MemberNamePredicate, T> getFirstOrDefaultDelegate,
            bool shouldReplicateWhenReturning,
            bool shouldCloneWhenReturning,
            string collectionNameForTracing)
            where T : PSMemberInfo
        {
            return new CollectionEntry<T>(
                getMembers,
                getMember,
                shouldReplicateWhenReturning,
                shouldCloneWhenReturning,
                collectionNameForTracing);
        }

        private static T DotNetGetFirstMemberOrDefaultDelegate<T>(PSObject msjObj, MemberNamePredicate predicate)
            where T : PSMemberInfo
        {
            throw null;
        }

        private T GetDotNetPropertyImpl<T>(object obj, string propertyName, MemberNamePredicate predicate = null)
            where T : PSMemberInfo
        {
            bool lookingForProperties = typeof(T).IsAssignableFrom(typeof(PSProperty));
            bool lookingForParameterizedProperties = IsTypeParameterizedProperty(typeof(T));
            if (!lookingForProperties && !lookingForParameterizedProperties)
            {
                return null;
            }

            CacheTable typeTable = _isStatic
                ? GetStaticPropertyReflectionTable((Type)obj)
                : GetInstancePropertyReflectionTable(obj.GetType());

            object entry = typeTable[propertyName];

            return entry switch
            {
                null => null,
                PropertyCacheEntry cacheEntry when lookingForProperties
                    => new PSProperty(
                        cacheEntry.member.Name,
                        this,
                        obj,
                        cacheEntry) as T,
                ParameterizedPropertyCacheEntry paramCacheEntry when lookingForParameterizedProperties
                    => new PSParameterizedProperty(
                        paramCacheEntry.propertyName,
                        this,
                        obj,
                        paramCacheEntry) as T,
                _ => null,
            };
        }

        private T GetDotNetMethodImpl<T>(object obj, string methodName, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                return null;
            }

            CacheTable typeTable = _isStatic
                ? GetStaticMethodReflectionTable((Type)obj)
                : GetInstanceMethodReflectionTable(obj.GetType());

            MethodCacheEntry methods = (MethodCacheEntry)typeTable[methodName];

            if (methods == null)
            {
                return null;
            }

            bool isCtor = methods[0].method is ConstructorInfo;
            bool isSpecial = !isCtor && methods[0].method.IsSpecialName;

            return new PSMethod(methods[0].method.Name, this, obj, methods, isSpecial, isHidden: false) as T;
        }
    }
}

namespace System.Management.Automation
{
    internal delegate bool MemberNamePredicate(string memberName);
}
