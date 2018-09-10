using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace ImpliedReflection
{
    internal class PSMemberData
    {
        private delegate void AddAllMembers(
            object baseObj,
            Type type,
            Members<PSMemberInfo> memberTable,
            MemberData memberData);

        private delegate void AddStaticMembers(
            Type type,
            MemberData memberData);

        private static readonly Dictionary<Type, PSMemberData> s_memberInfoCache =
            new Dictionary<Type, PSMemberData>();

        private static readonly AddAllMembers s_addAllMembers = CreateAddAllMembers();

        private static readonly AddStaticMembers s_addStaticMembers = CreateAddStaticMembers();

        private static readonly HashSet<Type> s_alreadyAddedStatic = new HashSet<Type>();

        private static readonly ConcurrentDictionary<string, bool> s_disallowReflectionCache = GetAdapterDisallowReflectionCache();

        public Members<PSMemberInfo> Members { get; }

        public Members<PSPropertyInfo> Properties { get; }

        public Members<PSMethodInfo> Methods { get; }

        private PSMemberData(MemberData memberData, object baseObj, Type type)
        {
            var coreDictionary = new Dictionary<string, PSMemberInfo>(StringComparer.OrdinalIgnoreCase);
            Members = new Members<PSMemberInfo>(coreDictionary);
            Properties = new Members<PSPropertyInfo>(coreDictionary);
            Methods = new Members<PSMethodInfo>(coreDictionary);
            s_addAllMembers(baseObj, type, Members, memberData);
        }

        public static PSMemberData Get(Type type, object baseObj)
        {
            // In PowerShell Core when member binders are creating the expression to invoke,
            // it's possible for it to cast the target expression as the closest public base
            // type. This is done the target expression type is either:
            //      - Within a framework assembly
            //      - Within an assembly decorated with DisablePrivateReflectionAttribute
            // The former definitely works, so maybe that was an early limitation in Core that
            // is no longer true. To get around this, we override the DotNetAdapter's private
            // to make everything we bind false.
            if (s_disallowReflectionCache != null)
            {
                s_disallowReflectionCache.AddOrUpdate(
                    type.Assembly.FullName,
                    addValue: false,
                    updateValueFactory: (name, currentValue) => false);
            }

            PSMemberData memberInfo;
            lock (s_memberInfoCache)
            {
                if (typeof(Type).IsAssignableFrom(type) && baseObj != null)
                {
                    if (!s_alreadyAddedStatic.Contains((Type)baseObj))
                    {
                        s_alreadyAddedStatic.Add((Type)baseObj);
                        s_addStaticMembers(
                            (Type)baseObj,
                            MemberData.Get((Type)baseObj, isStatic: true));
                    }
                }

                if (s_memberInfoCache.TryGetValue(type, out memberInfo))
                {
                    return memberInfo;
                }

                MemberData memberData = MemberData.Get(type, isStatic: false);
                memberInfo = new PSMemberData(memberData, baseObj, type);
                s_memberInfoCache.Add(type, memberInfo);
                if (!s_alreadyAddedStatic.Contains(type))
                {
                    s_alreadyAddedStatic.Add(type);
                    s_addStaticMembers(type, MemberData.Get(type, isStatic: true));
                }
            }

            return memberInfo;
        }

        private static AddStaticMembers CreateAddStaticMembers()
        {
            ParameterExpression type = Parameter(typeof(Type), nameof(type));
            ParameterExpression memberData = Parameter(typeof(MemberData), nameof(memberData));
            return Lambda<AddStaticMembers>(
                Block(
                    // Creates an expression similar to:
                    //      foreach (PropertyInfo property in memberData.Properties)
                    //      {
                    //          AddToCache<PSPropertyInfo>(
                    //              property.Name,
                    //              type,
                    //              NewPropertyCache(property, isStatic: true),
                    //              isStatic: true);
                    //      }
                    Expr.ForEach<PropertyInfo>(
                        Property(memberData, typeof(MemberData).GetProperty(nameof(MemberData.Properties), Bind.NonPublic.Instance)),
                        (current, @break, @continue) => Invoke(
                            Expr.AddToCache<PSPropertyInfo>(),
                            Property(current, nameof(MemberInfo.Name)),
                            type,
                            Expr.Factories.Property<PropertyInfo>.NewPropertyCache(current, Expr.True),
                            Expr.True)),
                    // Creates an expression similar to:
                    //      foreach (FieldInfo field in memberData.Fields)
                    //      {
                    //          AddToCache<PSPropertyInfo>(
                    //              field.Name,
                    //              type,
                    //              NewPropertyCache(field, isStatic: true),
                    //              isStatic: true);
                    //      }
                    Expr.ForEach<FieldInfo>(
                        Property(memberData, typeof(MemberData).GetProperty(nameof(MemberData.Fields), Bind.NonPublic.Instance)),
                        (current, @break, @continue) => Invoke(
                            Expr.AddToCache<PSPropertyInfo>(),
                            Property(current, nameof(MemberInfo.Name)),
                            type,
                            Expr.Factories.Property<FieldInfo>.NewPropertyCache(current, Expr.True),
                            Expr.True)),
                    // Creates an expression similar to:
                    //      foreach (KeyValuePair<string, IList<MethodBase>> methodGroup in memberData.Methods)
                    //      {
                    //          AddToCache<PSMethodInfo>(
                    //              methodGroup.Key,
                    //              type,
                    //              NewMethodCache(methodGroup.Value.ToArray(), isStatic: true),
                    //              isStatic: true);
                    //      }
                    Expr.ForEach<KeyValuePair<string, IList<MethodBase>>>(
                        Property(memberData, typeof(MemberData).GetProperty(nameof(MemberData.Methods), Bind.NonPublic.Instance)),
                        (current, @break, @continue) => Invoke(
                            Expr.AddToCache<PSMethodInfo>(),
                            Property(current, nameof(KeyValuePair<string, IList<MethodBase>>.Key)),
                            type,
                            Expr.Factories.Method.NewMethodCache(
                                Expr.ToArray<MethodBase>(
                                    Property(current, nameof(KeyValuePair<string, IList<MethodBase>>.Value)))),
                            Expr.True))),
                    new[] { type, memberData })
                    .Compile();
        }

        private static AddAllMembers CreateAddAllMembers()
        {
            ParameterExpression baseObj = Parameter(typeof(object), nameof(baseObj));
            ParameterExpression type = Parameter(typeof(Type), nameof(type));
            ParameterExpression memberTable = Parameter(typeof(Members<PSMemberInfo>), nameof(memberTable));
            ParameterExpression memberData = Parameter(typeof(MemberData), nameof(memberData));
            MethodInfo addMethod = typeof(Members<PSMemberInfo>).GetMethod(nameof(IDictionary.Add), new[] { typeof(string), typeof(PSMemberInfo) });

            return Lambda<AddAllMembers>(
                Block(
                    // Add fields first so any properties with the same name will take priority.
                    // Creates an expression similar to:
                    //      foreach (FieldInfo field in memberData.Fields)
                    //      {
                    //          memberTable.Add(
                    //              field.Name,
                    //              PropertyFactory(baseObj, type, field));
                    //      }
                    Expr.ForEach<FieldInfo>(
                        Property(memberData, typeof(MemberData).GetProperty(nameof(MemberData.Fields), Bind.NonPublic.Instance)),
                        (current, @break, @continue) => Call(
                            memberTable,
                            addMethod,
                            Property(current, typeof(MemberInfo).GetProperty(nameof(MemberInfo.Name))),
                            Invoke(Expr.Factories.Property<FieldInfo>.Factory, baseObj, type, current))),
                    // Creates an expression similar to:
                    //      foreach (PropertyInfo property in memberData.Properties)
                    //      {
                    //          memberTable.Add(
                    //              property.Name,
                    //              PropertyFactory(baseObj, type, property));
                    //      }
                    Expr.ForEach<PropertyInfo>(
                        Property(memberData, typeof(MemberData).GetProperty(nameof(MemberData.Properties), Bind.NonPublic.Instance)),
                        (current, @break, @continue) => Call(
                            memberTable,
                            addMethod,
                            Property(current, typeof(MemberInfo).GetProperty(nameof(MemberInfo.Name))),
                            Invoke(Expr.Factories.Property<PropertyInfo>.Factory, baseObj, type, current))),
                    // Creates an expression similar to:
                    //      foreach (KeyValuePair<string, IList<MethodBase> methodGroup in memberData.Methods)
                    //      {
                    //          memberTable.Add(
                    //              methodGroup.Key,
                    //              MethodFactory(baseObj, type, methodGroup.Value));
                    //      }
                    Expr.ForEach<KeyValuePair<string, IList<MethodBase>>>(
                        Property(memberData, typeof(MemberData).GetProperty(nameof(MemberData.Methods), Bind.NonPublic.Instance)),
                        (current, @break, @continue) => Call(
                            memberTable,
                            addMethod,
                            Property(current, nameof(KeyValuePair<string, IList<MethodBase>>.Key)),
                            Invoke(
                                Expr.Factories.Method.Factory,
                                baseObj,
                                type,
                                Property(current, nameof(KeyValuePair<string, IList<MethodBase>>.Value)))))),
                new[] { baseObj, type, memberTable, memberData })
                .Compile();
        }

        private static ConcurrentDictionary<string, bool> GetAdapterDisallowReflectionCache()
        {
            if (Cache.DotNetAdapter_s_disallowReflectionCache == null)
            {
                // This field only exists in (and only applies to) PowerShell Core.
                return null;
            }

            return (ConcurrentDictionary<string, bool>)Cache.DotNetAdapter_s_disallowReflectionCache.GetValue(null);
        }
    }
}
