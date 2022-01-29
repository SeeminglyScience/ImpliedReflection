using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
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
                (CollectionEntry<T>.GetFirstOrDefaultDelegate)getFirstOrDefaultDelegate.Method.CreateDelegate(typeof(CollectionEntry<T>.GetFirstOrDefaultDelegate)),
                shouldReplicateWhenReturning,
                shouldCloneWhenReturning,
                collectionNameForTracing);
        }

        private static T DotNetGetFirstMemberOrDefaultDelegate<T>(PSObject msjObj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            Adapter adapter = msjObj.InternalBaseDotNetAdapter;
            if (adapter is null || adapter.GetType() == typeof(DotNetAdapter))
            {
                adapter = s_customInstance;
            }

            return adapter.BaseGetFirstMemberOrDefault<T>(msjObj.ImmediateBaseObject, predicate);
        }

        protected override T GetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate)
        {
            return GetFirstDotNetPropertyOrDefault<T>(obj, predicate)
                ?? GetFirstDotNetMethodOrDefault<T>(obj, predicate)
                ?? GetFirstDotNetEventOrDefault<T>(obj, predicate)
                ?? GetFirstDynamicMemberOrDefault<T>(obj, predicate);
        }

        private T GetDotNetPropertyImpl<T>(object obj, string propertyName, MemberNamePredicate predicate)
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

            object entry = predicate != null
                ? typeTable.GetFirstOrDefault(predicate)
                : typeTable[propertyName];

            return entry switch
            {
                null => null,
                PropertyCacheEntry cacheEntry when lookingForProperties
                    => new PSProperty(
                        cacheEntry.member.Name,
                        this,
                        obj,
                        cacheEntry)
                        { IsHidden = cacheEntry.IsHidden } as T,
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

            MethodCacheEntry methods = predicate != null
                ? (MethodCacheEntry)typeTable.GetFirstOrDefault(predicate)
                : (MethodCacheEntry)typeTable[methodName];

            if (methods == null)
            {
                return null;
            }

            bool isCtor = methods[0].method is ConstructorInfo;
            bool isSpecial = !isCtor && methods[0].method.IsSpecialName;

            return PSMethod.Create(methods[0].method.Name, this, obj, methods, isSpecial, methods.IsHidden) as T;
        }

        private new T GetFirstDotNetPropertyOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            return GetDotNetPropertyImpl<T>(obj, propertyName: null, predicate);
        }

        private new T GetFirstDotNetMethodOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            return GetDotNetMethodImpl<T>(obj, methodName: null, predicate);
        }

        private new T GetFirstDotNetEventOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSEvent)))
            {
                return null;
            }

            Dictionary<string, EventCacheEntry> table = _isStatic
                ? GetStaticEventReflectionTable((Type)obj)
                : GetInstanceEventReflectionTable(obj.GetType());

            foreach (EventCacheEntry psEvent in table.Values)
            {
                if (predicate(psEvent.events[0].Name))
                {
                    return new PSEvent(psEvent.events[0]) as T;
                }
            }

            return null;
        }

        private new static T GetFirstDynamicMemberOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            if (obj is not IDynamicMetaObjectProvider idmop || obj is PSObject)
            {
                return null;
            }

            if (!typeof(T).IsAssignableFrom(typeof(PSDynamicMember)))
            {
                return null;
            }

            foreach (string name in idmop.GetMetaObject(Expression.Variable(idmop.GetType())).GetDynamicMemberNames())
            {
                if (predicate(name))
                {
                    return new PSDynamicMember(name) as T;
                }
            }

            return null;
        }
    }
}
