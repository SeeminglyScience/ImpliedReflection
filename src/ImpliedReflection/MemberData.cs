using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ImpliedReflection
{
    internal class MemberData
    {
        private const string PrivateConstructorProxyTypeSuffix = "_<privateConstructorProxies>";

        private delegate ParameterBuilder DefineParameter(
            DynamicMethod method,
            int position,
            ParameterAttributes attributes,
            string parameterName);

        private static readonly Dictionary<Type, MemberData> s_staticMemberCache = new Dictionary<Type, MemberData>();

        private static readonly Dictionary<Type, MemberData> s_instanceMemberCache = new Dictionary<Type, MemberData>();

        private static readonly Dictionary<DelegateTypes, Type> s_delegateTypeCache = new Dictionary<DelegateTypes, Type>();

        private static int s_generatedDelegateCount;

        private static readonly MemberData s_empty = new MemberData(
            Array.Empty<MemberInfo>(),
            isStatic: false);

        private static object s_syncObject = new object();

        private static ModuleBuilder _moduleBuilder;

        private static ModuleBuilder ModuleBuilder
        {
            get
            {
                lock(s_syncObject)
                {
                    if (_moduleBuilder != null)
                    {
                        return _moduleBuilder;
                    }

                    return _moduleBuilder = AssemblyBuilder
                        .DefineDynamicAssembly(
                            new AssemblyName(StringLiterals.DynamicProxyAssemblyName),
                            AssemblyBuilderAccess.Run)
                        .DefineDynamicModule(StringLiterals.DynamicProxyAssemblyName);
                }
            }
        }

        internal IReadOnlyDictionary<string, IList<MethodBase>> Methods { get; }

        internal ReadOnlyCollection<PropertyInfo> Properties { get; }

        internal ReadOnlyCollection<FieldInfo> Fields { get; }

        internal ReadOnlyCollection<EventInfo> Events { get; }

        internal bool IsStatic { get; }

        private MemberData(MemberInfo[] members, bool isStatic)
        {
            var properties = new ReadOnlyCollectionBuilder<PropertyInfo>();
            var fields = new ReadOnlyCollectionBuilder<FieldInfo>();
            var events = new ReadOnlyCollectionBuilder<EventInfo>();
            var methods = new Dictionary<string, IList<MethodBase>>(StringComparer.OrdinalIgnoreCase);

            foreach (MemberInfo member in members)
            {
                switch (member)
                {
                    case PropertyInfo property: properties.Add(property); break;
                    case FieldInfo field: fields.Add(field); break;
                    case MethodInfo method: AddMethod(methods, method); break;
                    case ConstructorInfo constructor: AddMethod(methods, constructor); break;
                    case EventInfo @event: events.Add(@event); break;
                }
            }

            if (methods.TryGetValue(StringLiterals.ProxyCtorName, out IList<MethodBase> privateCtors))
            {
                methods[StringLiterals.ProxyCtorName] = ConvertToConstructorProxies(privateCtors);
            }

            Properties = properties.ToReadOnlyCollection();
            Fields = fields.ToReadOnlyCollection();
            Events = events.ToReadOnlyCollection();
            Methods = methods;
            IsStatic = isStatic;
        }

        internal static MemberData Get(Type type, bool isStatic = false)
        {
            return GetOrCreateMemberData(type, isStatic);
        }

        private static MemberData GetOrCreateMemberData(Type type, bool isStatic = false)
        {
            if (type == null)
            {
                return s_empty;
            }

            if (type.IsDefined(typeof(Internal.GeneratedProxyHelpersAttribute), inherit: false))
            {
                return s_empty;
            }

            Dictionary<Type, MemberData> cache = isStatic ? s_staticMemberCache : s_instanceMemberCache;
            MemberData memberData = null;
            lock (cache)
            {
                if (cache.TryGetValue(type, out memberData))
                {
                    return memberData;
                }

                if (isStatic)
                {
                    memberData = new MemberData(
                        type.GetMembers(Bind.Any.Static)
                            .Where(m => !(m is ConstructorInfo))
                            .Concat(type.GetConstructors(Bind.Any.Instance))
                            .ToArray(),
                        isStatic: true);
                }
                else
                {
                    memberData = new MemberData(
                        type.GetMembers(Bind.Any.Instance)
                            .Where(m => !(m is ConstructorInfo))
                            .ToArray(),
                        isStatic: false);
                }

                cache.Add(type, memberData);
            }

            return memberData;
        }

        private static void AddMethod(
            Dictionary<string, IList<MethodBase>> dictionary,
            MethodBase method)
        {
            string methodName = null;
            if (method.Name.Equals(StringLiterals.ReflectionCtorName, StringComparison.Ordinal))
            {
                methodName = method.IsPublic ? StringLiterals.PSCtorName : StringLiterals.ProxyCtorName;
            }
            else
            {
                methodName = method.Name;
            }

            if (dictionary.TryGetValue(methodName, out IList<MethodBase> collection))
            {
                collection.Add(method);
                return;
            }

            dictionary.Add(methodName, new List<MethodBase>() { method });
        }

        private static MethodBase[] ConvertToConstructorProxies(IList<MethodBase> originalMethods)
        {
            // Binding non-public constructors ended up being far more complicated than I expected.
            // Initially I attemped to add bind them the same way we bind all other static methods,
            // but unfortunately the PSCreateInstanceBinder doesn't _really_ use the cache. Instead
            // it calls Type.GetConstructors, which will only find public constructors and ignore
            // the cache completely.
            //
            // So I had the idea to create a DynamicMethod that would call the constructor and add
            // that to the cache. Unfortunately the PowerShell binder checks MethodBase.DeclaringType
            // at a few different points, which ends up throwing a NullReferenceException.
            //
            // That's when I decided I needed to dynamically create a class to store the static
            // methods and get past the binder. Easy enough, unfortunately you can't skip visibility
            // (i.e. Public/Protected/etc) checks with MethodBuilder like you can with DynamicMethod.
            //
            // Finally, I decided to go with saving the original DynamicMethod to a static field as
            // a delegate and having the generated method invoke that delegate. I'm not particularly
            // happy with this, but it works.
            MethodBase[] newMethods = new MethodBase[originalMethods.Count];
            MethodBuilder[] methodBuilders = ArrayPool<MethodBuilder>.Shared.Rent(newMethods.Length);
            DynamicMethod[] methodImpls = ArrayPool<DynamicMethod>.Shared.Rent(newMethods.Length);
            FieldBuilder[] fieldBehinds = ArrayPool<FieldBuilder>.Shared.Rent(newMethods.Length);
            Type[][] parameterTypes = ArrayPool<Type[]>.Shared.Rent(newMethods.Length);
            try
            {
                TypeBuilder typeBuilder = ModuleBuilder.DefineType(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0}{1}_{2}",
                        originalMethods[0].ReflectedType.FullName,
                        PrivateConstructorProxyTypeSuffix,
                        originalMethods[0].ReflectedType.GetTypeInfo().GetHashCode().ToString("x")),
                    TypeAttributes.NotPublic | TypeAttributes.Abstract,
                    typeof(object));

                typeBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        Cache.GeneratedProxyHelpersAttribute_ctor,
                        Array.Empty<object>()));

                for (var i = 0; i < newMethods.Length; i++)
                {
                    CreateMethodForPrivateConstructor(
                        (ConstructorInfo)originalMethods[i],
                        typeBuilder,
                        out methodBuilders[i],
                        out methodImpls[i],
                        out fieldBehinds[i],
                        out parameterTypes[i]);
                }

                Type constructedType = typeBuilder.CreateTypeInfo().AsType();
                for (var i = 0; i < newMethods.Length; i++)
                {
                    newMethods[i] = constructedType
                        .GetMethod(
                            StringLiterals.ProxyCtorName,
                            Bind.NonPublic.Static,
                            null,
                            parameterTypes[i],
                            null);

                    FieldInfo constructedField = constructedType.GetField(fieldBehinds[i].Name, Bind.Public.Static);

                    constructedField.SetValue(
                        obj: null,
                        value: methodImpls[i].CreateDelegate(constructedField.FieldType));
                }

                return newMethods;
            }
            finally
            {
                ArrayPool<MethodBuilder>.Shared.Return(methodBuilders);
                ArrayPool<DynamicMethod>.Shared.Return(methodImpls);
                ArrayPool<FieldInfo>.Shared.Return(fieldBehinds);
                ArrayPool<Type[]>.Shared.Return(parameterTypes);
            }
        }

        private static void CreateMethodForPrivateConstructor(
            ConstructorInfo constructor,
            TypeBuilder typeBuilder,
            out MethodBuilder proxyMethod,
            out DynamicMethod implementation,
            out FieldBuilder field,
            out Type[] parameterTypes)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            parameterTypes = new Type[parameters.Length];
            var delegateTypeArguments = new Type[parameters.Length + 1];
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
                delegateTypeArguments[i] = parameters[i].ParameterType;
            }

            // If the method tries to return a non-public class it will throw a access
            // violation exception.
            Type returnType = constructor.ReflectedType.IsPublic
                ? constructor.ReflectedType
                : typeof(object);

            delegateTypeArguments[delegateTypeArguments.Length - 1] = returnType;

            proxyMethod = typeBuilder.DefineMethod(
                StringLiterals.ProxyCtorName,
                MethodAttributes.Private | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes);

            proxyMethod.SetImplementationFlags(MethodImplAttributes.IL | MethodImplAttributes.NoInlining);

            Type delegateType = GetOrCreateDelegateType(parameterTypes, delegateTypeArguments);
            field = typeBuilder.DefineField(
                string.Format(
                    "{0}_{1}",
                    StringLiterals.ProxyCtorName,
                    constructor.GetHashCode()),
                delegateType,
                FieldAttributes.Static | FieldAttributes.Public);

            implementation = new DynamicMethod(
                StringLiterals.ProxyCtorName,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                ModuleBuilder,
                skipVisibility: true);

            ILGenerator il = proxyMethod.GetILGenerator();
            ILGenerator dynamicMethodIl = implementation.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, field);
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                ParameterBuilder builder = proxyMethod.DefineParameter(
                    i + 1,
                    ParameterAttributes.In,
                    parameters[i].Name);

                foreach (CustomAttributeData attribute in parameters[i].GetCustomAttributesData())
                {
                    builder.SetCustomAttribute(ToBuilder(attribute));
                }

                switch (i)
                {
                    case 0: il.Emit(OpCodes.Ldarg_0); dynamicMethodIl.Emit(OpCodes.Ldarg_0); break;
                    case 1: il.Emit(OpCodes.Ldarg_1); dynamicMethodIl.Emit(OpCodes.Ldarg_1); break;
                    case 2: il.Emit(OpCodes.Ldarg_2); dynamicMethodIl.Emit(OpCodes.Ldarg_2); break;
                    case 3: il.Emit(OpCodes.Ldarg_3); dynamicMethodIl.Emit(OpCodes.Ldarg_3); break;
                    default: il.Emit(OpCodes.Ldarg, i); dynamicMethodIl.Emit(OpCodes.Ldarg, i); break;
                }
            }

            dynamicMethodIl.Emit(OpCodes.Newobj, constructor);
            if (returnType == typeof(object) && constructor.ReflectedType.IsValueType)
            {
                dynamicMethodIl.Emit(OpCodes.Box, constructor.ReflectedType);
            }

            dynamicMethodIl.Emit(OpCodes.Ret);
            il.Emit(OpCodes.Callvirt, delegateType.GetMethod(nameof(Action.Invoke)));
            il.Emit(OpCodes.Ret);
        }

        private static Type GetOrCreateDelegateType(Type[] parameterTypes, Type[] delegateTypeArguments)
        {
            var delegateSignature = new DelegateTypes(delegateTypeArguments);
            Type delegateType;
            lock (s_delegateTypeCache)
            {
                if (s_delegateTypeCache.TryGetValue(delegateSignature, out delegateType))
                {
                    return delegateType;
                }

                if (!ContainsInvalidGenericArgument(delegateTypeArguments))
                {
                    delegateType = Expression.GetFuncType(delegateTypeArguments);
                    s_delegateTypeCache.Add(delegateSignature, delegateType);
                    return delegateType;
                }

                int id = Interlocked.Increment(ref s_generatedDelegateCount);
                TypeBuilder typeBuilder = ModuleBuilder.DefineType(
                    $"generatedDelegate_<{id}>",
                    TypeAttributes.Public | TypeAttributes.Sealed,
                    typeof(MulticastDelegate));

                MethodBuilder invoke = typeBuilder.DefineMethod(
                    nameof(Action.Invoke),
                    MethodAttributes.Public |
                        MethodAttributes.HideBySig |
                        MethodAttributes.NewSlot |
                        MethodAttributes.Virtual,
                    delegateTypeArguments[delegateTypeArguments.Length - 1],
                    parameterTypes);

                invoke.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

                ConstructorBuilder constructor = typeBuilder.DefineConstructor(
                    MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
                    CallingConventions.Standard,
                    new[] { typeof(object), typeof(IntPtr) });

                constructor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

                delegateType = typeBuilder.CreateTypeInfo().AsType();
                s_delegateTypeCache.Add(delegateSignature, delegateType);
                return delegateType;
            }
        }

        private static bool ContainsInvalidGenericArgument(Type[] arguments)
        {
            foreach (Type typeArgument in arguments)
            {
                if (typeArgument.IsPointer | typeArgument.IsByRef)
                {
                    return true;
                }

                if (Cache.IsByRefLikeAttribute != null &&
                    typeArgument.IsDefined(Cache.IsByRefLikeAttribute, inherit: false))
                {
                    return true;
                }
            }

            return false;
        }

        private static CustomAttributeBuilder ToBuilder(CustomAttributeData attribute)
        {
            var constructorArguments = new object[attribute.ConstructorArguments.Count];
            for (var i = 0; i < attribute.ConstructorArguments.Count; i++)
            {
                constructorArguments[i] = attribute.ConstructorArguments[i].Value;
            }

            if (attribute.NamedArguments.Count == 0)
            {
                return new CustomAttributeBuilder(attribute.Constructor, constructorArguments);
            }

            var fields = new List<FieldInfo>();
            var fieldValues = new List<object>();
            var properties = new List<PropertyInfo>();
            var propertyValues = new List<object>();

            foreach (CustomAttributeNamedArgument namedArgument in attribute.NamedArguments)
            {
                if (namedArgument.MemberInfo is PropertyInfo property)
                {
                    properties.Add(property);
                    propertyValues.Add(namedArgument.TypedValue.Value);
                    continue;
                }

                fields.Add((FieldInfo)namedArgument.MemberInfo);
                fieldValues.Add(namedArgument.TypedValue.Value);
            }

            return new CustomAttributeBuilder(
                attribute.Constructor,
                constructorArguments,
                properties.ToArray(),
                propertyValues.ToArray(),
                fields.ToArray(),
                fieldValues.ToArray());
        }

        private readonly struct DelegateTypes
        {
            private readonly int GenericArgumentsHash;

            internal DelegateTypes(Type[] genericTypeArguments)
            {
                GenericArgumentsHash = genericTypeArguments.SequenceGetHashCode();
            }
        }
    }
}
