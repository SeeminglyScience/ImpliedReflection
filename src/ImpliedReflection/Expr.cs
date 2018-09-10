using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Reflection;

using static System.Linq.Expressions.Expression;

namespace ImpliedReflection
{
    internal delegate Expression ForEachBodyCreator(
        Expression current,
        LabelTarget @break,
        LabelTarget @continue);

    internal delegate PSProperty PropertyFactory<in TMemberType>(
        object baseObject,
        Type type,
        TMemberType source)
        where TMemberType : MemberInfo;

    internal delegate PSMethod MethodFactory(
        object baseObject,
        Type type,
        IList<MethodBase> source);


    internal static class Expr
    {
        internal static ConstantExpression True = Constant(true, typeof(bool));

        internal static ConstantExpression False = Constant(false, typeof(bool));

        internal static ConstantExpression ObjectNull = Constant(null);

        internal static ConstantExpression Null<T>() => NullExpressions<T>.Instance;

        private static class NullExpressions<T>
        {
            internal static ConstantExpression Instance = Constant(null, typeof(T));
        }

        internal static class Factories
        {
            internal static class Method
            {
                internal static Expression<MethodFactory> Factory = CreateFactory();

                internal static Expression NewMethodCache(Expression methodArray)
                {
                    ParameterExpression methodEntry = Variable(Cache.MethodCacheEntry);
                    return New(Cache.MethodCacheEntry_ctor, methodArray);
                }

                private static Expression<MethodFactory> CreateFactory()
                {
                    ParameterExpression methods = Parameter(typeof(IList<MethodBase>));
                    ParameterExpression baseObj = Parameter(typeof(object));
                    ParameterExpression type = Parameter(typeof(Type));
                    ParameterExpression methodArray = Variable(typeof(MethodBase[]));
                    ParameterExpression cacheEntry = Variable(Cache.MethodCacheEntry);
                    ParameterExpression isSpecial = Variable(typeof(bool));
                    BinaryExpression firstMethod = ArrayIndex(methodArray, Expression.Constant(0));

                    return Lambda<MethodFactory>(
                        Block(
                            new[] { cacheEntry, methodArray, isSpecial },
                            // Creates an expression like:
                            //      MethodBase[] methodArray = methods.ToArray();
                            Assign(methodArray, ToArray<MethodBase>(methods)),
                            // Creates an expression like:
                            //      MethodCacheEntry cacheEntry = NewMethodCache(methodArray); // inlined
                            Assign(cacheEntry, NewMethodCache(methodArray)),
                            // Creates an expression like:
                            //      AddToCache<PSMethodInfo>(GetMemberName(firstMethod), type, cacheEntry, false);
                            Invoke(
                                AddToCache<PSMethodInfo>(),
                                GetMemberName(firstMethod),
                                type,
                                cacheEntry,
                                Expr.False),
                            // Creates an expression like:
                            //      bool isSpecial = firstMethod.Attributes.HasFlag(MethodAttributes.SpecialName);
                            Assign(
                                isSpecial,
                                Call(
                                    Property(
                                        firstMethod,
                                        typeof(MethodInfo).GetProperty(nameof(MethodInfo.Attributes))),
                                    typeof(Enum).GetMethod(
                                        nameof(Enum.HasFlag),
                                        new[] { typeof(Enum) }),
                                    Convert(
                                        Field(
                                            null,
                                            typeof(MethodAttributes)
                                                .GetField(nameof(MethodAttributes.SpecialName))),
                                        typeof(Enum)))),
                            // Creates an expression like:
                            //      return new PSMethod(
                            //          firstMethod.Name,
                            //          PSObject.dotNetInstanceAdapter,
                            //          baseObj,
                            //          cacheEntry,
                            //          isSpecial,
                            //          isSpecial);
                            Expression.New(
                                Cache.PSMethod_ctor,
                                Property(firstMethod, nameof(MethodInfo.Name)),
                                Expression.Field(null, Cache.PSObject_dotNetInstanceAdapter),
                                baseObj,
                                cacheEntry,
                                isSpecial,
                                isSpecial)),
                        new[] { baseObj, type, methods });
                }
            }

            internal static class Property<TMemberType>
                where TMemberType : MemberInfo
            {
                internal static Expression<PropertyFactory<TMemberType>> Factory = CreateFactory();

                internal static Expression NewPropertyCache(Expression property, Expression isStatic)
                {
                    // Creates an expression like:
                    //      new PropertyCacheEntry(property);
                    if (typeof(TMemberType) == typeof(FieldInfo))
                    {
                        return New(Cache.PropertyCacheEntry_ctor_FieldInfo, property);
                    }

                    return New(Cache.PropertyCacheEntry_ctor_PropertyInfo, property);
                }

                private static Expression IsWriteOnly(Expression property)
                {
                    if (typeof(TMemberType) == typeof(FieldInfo))
                    {
                        return Expr.False;
                    }

                    // Creates an expression similar to:
                    //      property.GetGetMethod(nonPublic: true) == null;
                    return IsNull(Call(property, Cache.PropertyInfo_GetGetMethod, Expr.True));
                }

                private static Expression IsReadOnly(Expression property)
                {
                    if (typeof(TMemberType) == typeof(FieldInfo))
                    {
                        // Creates an expression similar to:
                        //      property.IsInitOnly || property.IsLiteral;
                        return OrElse(
                            Property(property, nameof(FieldInfo.IsInitOnly)),
                            Property(property, nameof(FieldInfo.IsLiteral)));
                    }

                    // Creates an expression similar to:
                    //      property.GetSetMethod(nonPublic: true) == null;
                    return IsNull(Call(property, Cache.PropertyInfo_GetSetMethod, Expr.True));
                }

                private static Expression<PropertyFactory<TMemberType>> CreateFactory()
                {
                    ParameterExpression property = Parameter(typeof(TMemberType));
                    ParameterExpression baseObj = Parameter(typeof(object));
                    ParameterExpression type = Parameter(typeof(Type));
                    ParameterExpression cacheEntry = Variable(Cache.PropertyCacheEntry);
                    LabelTarget returnLabel = Label(typeof(PSProperty));

                    return Lambda<PropertyFactory<TMemberType>>(
                        Block(
                            new[] { cacheEntry },
                            // Creates an expression like:
                            //      PropertyCacheEntry cacheEntry = NewPropertyCache(property, false);
                            Assign(
                                cacheEntry,
                                NewPropertyCache(property, Expr.False)),
                            // Creates an expression like:
                            //      cacheEntry.writeOnly = IsWriteOnly(property);
                            Assign(
                                Field(cacheEntry, Cache.PropertyCacheEntry_writeOnly),
                                IsWriteOnly(property)),
                            // Creates an expression like:
                            //      cacheEntry.ReadOnly = IsReadOnly(property);
                            Assign(
                                Field(cacheEntry, Cache.PropertyCacheEntry_readOnly),
                                IsReadOnly(property)),
                            // Creates an expression like:
                            //      AddToCache<PSPropertyInfo>(
                            //          property.Name,
                            //          type,
                            //          cacheEntry,
                            //          false);
                            Invoke(
                                AddToCache<PSPropertyInfo>(),
                                Property(property, nameof(MemberInfo.Name)),
                                type,
                                cacheEntry,
                                Expr.False),
                            // Creates an expression like:
                            //      new PSProperty(
                            //          property.Name,
                            //          PSObject.dotNetInstanceAdapter,
                            //          baseObj,
                            //          cacheEntry);
                            New(
                                Cache.PSProperty_ctor,
                                Property(property, typeof(MemberInfo).GetProperty(nameof(MemberInfo.Name))),
                                Expression.Field(null, Cache.PSObject_dotNetInstanceAdapter),
                                baseObj,
                                cacheEntry)),
                        new[] { baseObj, type, property });
                }
            }
        }

        internal static BlockExpression ForEach<T>(Expression collection, ForEachBodyCreator body)
        {
            ParameterExpression enumerator = Variable(typeof(IEnumerator<T>));
            LabelTarget @continue = Label();
            LabelTarget @break = Label();

            // Creates an expression like:
            // IEnumerator<t> enumerator = collection.GetEnumerator();
            // try
            // {
            //      while (enumerator.MoveNext())
            //      {
            //          body(enumerator.Current);
            //      }
            // }
            // finally
            // {
            //      enumerator?.Dispose();
            // }
            return Block(
                new[] { enumerator },
                Assign(enumerator, GetEnumerator<T>(collection)),
                TryFinally(
                    Loop(
                        IfThenElse(
                            Call(enumerator, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))),
                            body(
                                Property(enumerator, typeof(IEnumerator<T>).GetProperty(nameof(IEnumerator<T>.Current))),
                                @break,
                                @continue),
                            Break(@break)),
                        @break,
                        @continue),
                    IfThen(
                        Equal(enumerator, Expr.Null<IEnumerator<T>>()),
                        Call(enumerator, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))))));
        }

        internal static Expression IsNull(Expression test)
        {
            return Equal(test, ObjectNull);
        }

        internal static BlockExpression ForEach(Type type, Expression collection, ForEachBodyCreator body)
        {
            ParameterExpression enumerator = Variable(typeof(IEnumerator<>).MakeGenericType(type));
            LabelTarget @continue = Label();
            LabelTarget @break = Label();

            // Creates an expression like:
            // IEnumerator<t> enumerator = collection.GetEnumerator();
            // try
            // {
            //      while (enumerator.MoveNext())
            //      {
            //          body(enumerator.Current);
            //      }
            // }
            // finally
            // {
            //      enumerator?.Dispose();
            // }
            return Block(
                new[] { enumerator },
                Assign(enumerator, GetEnumerator(type, collection)),
                TryFinally(
                    Loop(
                        IfThenElse(
                            Call(enumerator, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))),
                            body(
                                Property(enumerator, enumerator.Type.GetProperty(nameof(IEnumerator.Current))),
                                @break,
                                @continue),
                            Break(@break)),
                        @break,
                        @continue),
                    IfThen(
                        Equal(enumerator, Constant(null, typeof(IEnumerator<>).MakeGenericType(type))),
                        Call(enumerator, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))))));
        }

        internal static MethodCallExpression GetEnumerator<T>(Expression expression)
        {
            // Creates an expression like:
            //      expression.GetEnumerator();
            return Call(
                expression,
                typeof(IEnumerable<T>)
                    .GetMethod(nameof(IEnumerable<T>.GetEnumerator)));
        }

        internal static MethodCallExpression GetEnumerator(Type type, Expression expression)
        {
            // Creates an expression like:
            //      expression.GetEnumerator();
            return Call(
                expression,
                typeof(IEnumerable<>)
                    .MakeGenericType(type)
                    .GetMethod(nameof(IEnumerable.GetEnumerator)));
        }

        internal static MemberExpression Base(Expression pso)
        {
            // Creates an expression like:
            //      pso.BaseObject;
            return Property(pso, Cache.PSObject_BaseObject);
        }

        internal static Expression GetRuntimeType(Expression expression)
        {
            // Creates an expression like:
            //      expression.GetType();
            return Call(expression, Cache.Object_GetType);
        }

        private static Expression GetMemberName(Expression memberInfo)
        {
            // Creates an expression like:
            //      memberInfo.Name.Equals(".ctor", StringComparison.Ordinal)
            //          ? memberInfo.IsPublic ? "new" : "ctor"
            //          : memberInfo.Name;
            return Condition(
                Call(
                    Property(memberInfo, nameof(MemberInfo.Name)),
                    Cache.string_Equals,
                    Constant(StringLiterals.ReflectionCtorName),
                    Field(null, typeof(StringComparison), nameof(StringComparison.Ordinal))),
                Condition(
                    Property(memberInfo, nameof(MethodBase.IsPublic)),
                        Constant(StringLiterals.PSCtorName),
                        Constant(StringLiterals.ProxyCtorName)),
                Property(memberInfo, nameof(MemberInfo.Name)));
        }

        private static Expression GetCacheTable<TMemberType>(Expression type, Expression isStatic)
            where TMemberType : PSMemberInfo
        {
            Type cacheTableType = typeof(Dictionary<,>).MakeGenericType(typeof(Type), Cache.CacheTable);
            ParameterExpression cacheTable = Variable(Cache.CacheTable, nameof(cacheTable));
            LabelTarget returnLabel = Label(Cache.CacheTable);

            Expression cacheTableCache = null;
            if (typeof(TMemberType) == typeof(PSMethodInfo))
            {
                // Creates an expression like:
                //      isStatic ? DotNetAdapter.staticMethodCacheTable : DotNetAdapter.instanceMethodCacheTable;
                cacheTableCache = Condition(
                    isStatic,
                    Field(null, Cache.DotNetAdapter_staticMethodCacheTable),
                    Field(null, Cache.DotNetAdapter_instanceMethodCacheTable));
            }
            else if (typeof(TMemberType) == typeof(PSPropertyInfo))
            {
                // Creates an expression similar to:
                //      isStatic ? DotNetAdapter.staticPropertyCacheTable : DotNetAdapter.instancePropertyCacheTable;
                cacheTableCache = Condition(
                    isStatic,
                    Field(null, Cache.DotNetAdapter_staticPropertyCacheTable),
                    Field(null, Cache.DotNetAdapter_instancePropertyCacheTable));
            }
            else if (typeof(TMemberType) == typeof(PSEvent))
            {
                // Creates an expression similar to:
                //      isStatic ? DotNetAdapter.staticEventCacheTable : DotNetAdapter.instanceEventCacheTable;
                cacheTableCache = Condition(
                    isStatic,
                    Field(null, Cache.DotNetAdapter_staticEventCacheTable),
                    Field(null, Cache.DotNetAdapter_instanceEventCacheTable));
            }

            // Creates an expression similar to:
            //      if (DotNetAdapter.instancePropertyCacheTable.TryGetValue(type, out cacheTable))
            //      {
            //          return cacheTable;
            //      }
            Expression returnIfEntryAlreadyExists =
                IfThen(
                    Call(
                        cacheTableCache,
                        cacheTableType.GetMethod("TryGetValue"),
                        type,
                        cacheTable),
                    Return(returnLabel, cacheTable));

            // Creates an expression similar to:
            //      cacheTable = new CacheTable();
            //      DotNetAdapter.instancePropertyCacheTable.Add(type, cacheTable);
            //      return cacheTable;
            Expression otherwiseCreateNew =
                Block(
                    Assign(
                        cacheTable,
                        New(Cache.CacheTable)),
                    Call(
                        cacheTableCache,
                        cacheTableType.GetMethod("Add", cacheTableType.GetGenericArguments()),
                        type,
                        cacheTable));

            return Block(
                type: Cache.CacheTable,
                variables: new[] { cacheTable },
                new[] { returnIfEntryAlreadyExists, otherwiseCreateNew, Label(returnLabel, cacheTable) });
        }

        internal static Expression<Action<string, Type, object, bool>> AddToCache<TMemberType>()
            where TMemberType : PSMemberInfo
        {
            ParameterExpression memberName = Parameter(typeof(string));
            ParameterExpression type = Parameter(typeof(Type));
            ParameterExpression cacheEntry = Parameter(typeof(object));
            ParameterExpression isStatic = Parameter(typeof(bool));
            LabelTarget returnLabel = Label(typeof(void));

            ParameterExpression cacheTable = Variable(Cache.CacheTable);
            ParameterExpression memberIndex = Variable(typeof(int));

            // Creates an expression similar to:
            //      if (cacheTable.indexes.TryGetValue(memberName, out memberIndex))
            //      {
            //          cacheTable.memberCollection[memberIndex] = cacheEntry;
            //      }
            //      else
            //      {
            //          cacheTable.Add(memberName, cacheEntry);
            //      }
            Expression doesContainExpr = IfThenElse(
                Call(
                    Field(cacheTable, Cache.CacheTable_indexes),
                    Cache.CacheTable_indexes.FieldType.GetMethod("TryGetValue"),
                    memberName,
                    memberIndex),
                Call(
                    Field(cacheTable, Cache.CacheTable_memberCollection),
                    Cache.CacheTable_memberCollection.FieldType.GetMethod("set_Item"),
                    memberIndex,
                    cacheEntry),
                Call(
                    cacheTable,
                    Cache.CacheTable_Add,
                    memberName,
                    cacheEntry));

            return Lambda<Action<string, Type, object, bool>>(
                Block(
                    new[] { cacheTable, memberIndex },
                    Assign(
                        cacheTable,
                        GetCacheTable<TMemberType>(type, isStatic)),
                    doesContainExpr),
                new[] { memberName, type, cacheEntry, isStatic });
        }

        internal static Expression ToArray<TElementType>(Expression collection)
        {
            // Creates an expression similar to:
            //      if (collection is TElementType[] asArray)
            //      {
            //          return asArray;
            //      }
            //
            //      return collection.ToArray<TElementType>();
            if (collection.Type.IsArray && typeof(TElementType).IsAssignableFrom(collection.Type.GetElementType()))
            {
                return collection;
            }

            if (typeof(IEnumerable<TElementType>).IsAssignableFrom(collection.Type))
            {
                return Call(
                    typeof(Enumerable),
                    nameof(Enumerable.ToArray),
                    new[] { typeof(TElementType) },
                    collection);
            }

            throw new ArgumentException(nameof(collection));
        }
    }
}
