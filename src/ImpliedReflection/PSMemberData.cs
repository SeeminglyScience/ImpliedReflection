using System;
using System.Collections;
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

        public Members<PSMemberInfo> Members { get; }

        public Members<PSPropertyInfo> Properties { get; }

        public Members<PSMethodInfo> Methods { get; }

        private PSMemberData(MemberData memberData, object baseObj)
        {
            var coreDictionary = new Dictionary<string, PSMemberInfo>(StringComparer.OrdinalIgnoreCase);
            Members = new Members<PSMemberInfo>(coreDictionary);
            Properties = new Members<PSPropertyInfo>(coreDictionary);
            Methods = new Members<PSMethodInfo>(coreDictionary);
            s_addAllMembers(baseObj, Members, memberData);
        }

        public static PSMemberData Get(Type type, object baseObj)
        {
            // In PS6.1 trying to bind members of RuntimeType directly breaks member binding
            // for all System.Type subclasses. My guess is PowerShell now explicitly casts all
            // Type objects as TypeInfo in a binder somewhere _after_ already retrieving members
            // of RuntimeType.
            if (type == Cache.RuntimeType)
            {
                type = typeof(TypeInfo);
            }

            PSMemberData memberInfo;
            if (typeof(Type).IsAssignableFrom(type))
            {
                lock (s_alreadyAddedStatic)
                {
                    if (!s_alreadyAddedStatic.Contains((Type)baseObj))
                    {
                        s_addStaticMembers(
                            (Type)baseObj,
                            MemberData.Get((Type)baseObj, isStatic: true));
                        s_alreadyAddedStatic.Add((Type)baseObj);
                    }
                }
            }

            lock (s_memberInfoCache)
            {
                if (s_memberInfoCache.TryGetValue(type, out memberInfo))
                {
                    return memberInfo;
                }

                MemberData memberData = MemberData.Get(type, isStatic: false);
                memberInfo = new PSMemberData(memberData, baseObj);
                s_memberInfoCache.Add(type, memberInfo);
                if (!s_alreadyAddedStatic.Contains(type))
                {
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
                    //              PropertyFactory(baseObj, field));
                    //      }
                    Expr.ForEach<FieldInfo>(
                        Property(memberData, typeof(MemberData).GetProperty(nameof(MemberData.Fields), Bind.NonPublic.Instance)),
                        (current, @break, @continue) => Call(
                            memberTable,
                            addMethod,
                            Property(current, typeof(MemberInfo).GetProperty(nameof(MemberInfo.Name))),
                            Invoke(Expr.Factories.Property<FieldInfo>.Factory, baseObj, current))),
                    // Creates an expression similar to:
                    //      foreach (PropertyInfo property in memberData.Properties)
                    //      {
                    //          memberTable.Add(
                    //              property.Name,
                    //              PropertyFactory(baseObj, property));
                    //      }
                    Expr.ForEach<PropertyInfo>(
                        Property(memberData, typeof(MemberData).GetProperty(nameof(MemberData.Properties), Bind.NonPublic.Instance)),
                        (current, @break, @continue) => Call(
                            memberTable,
                            addMethod,
                            Property(current, typeof(MemberInfo).GetProperty(nameof(MemberInfo.Name))),
                            Invoke(Expr.Factories.Property<PropertyInfo>.Factory, baseObj, current))),
                    // Creates an expression similar to:
                    //      foreach (KeyValuePair<string, IList<MethodBase> methodGroup in memberData.Methods)
                    //      {
                    //          memberTable.Add(
                    //              methodGroup.Key,
                    //              MethodFactory(baseObj, methodGroup.Value));
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
                                Property(current, nameof(KeyValuePair<string, IList<MethodBase>>.Value)))))),
                new[] { baseObj, memberTable, memberData })
                .Compile();
        }
    }
}
