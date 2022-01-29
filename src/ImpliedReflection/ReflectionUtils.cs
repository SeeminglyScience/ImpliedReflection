using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ImpliedReflection
{
    // I don't think there is actually any real benefit to doing this like this.
    // Especially not in the abstracted way I've done it here. But hey it works
    // and it's neat. Don't do this though.
    internal static class Reflect
    {
        private readonly struct Key : IEquatable<Key>
        {
            public readonly Type Type;

            public readonly string Name;

            public Key(Type type, string name)
            {
                Type = type;
                Name = name;
            }

            public readonly bool Equals(Key other) => Type == other.Type && Name.Equals(other.Name, StringComparison.Ordinal);

            public readonly override bool Equals(object obj) => obj is Key other && Equals(other);

            public readonly override int GetHashCode() => HashCode.Combine(Type, Name);
        }

        private static readonly ConcurrentDictionary<Key, object> s_delegateCache = new();

        public delegate ref T GetRef<T>();

        public delegate ref TValue GetRef<TSource, TValue>(ref TSource source);

        public delegate IntPtr IntermediateGetRef<T>(ref T source);

        public static ref TValue StaticFieldRef<TSource, TValue>(string fieldName)
        {
            return ref StaticFieldRef<TValue>(typeof(TSource), fieldName);
        }

        public static ref T StaticFieldRef<T>(Type type, string fieldName)
        {
            object getter = s_delegateCache.GetOrAdd(
                new Key(type, fieldName),
                static key =>
                {
                    DynamicMethod method = new(
                        "GetFieldRef",
                        typeof(IntPtr),
                        Type.EmptyTypes,
                        typeof(Reflect).Module,
                        skipVisibility: true);

                    ILGenerator il = method.GetILGenerator();
                    il.Emit(OpCodes.Ldsflda, key.Type.GetField(key.Name, BindingFlags.Static | BindingFlags.NonPublic));
                    il.Emit(OpCodes.Ret);
                    return method.CreateDelegate(typeof(Func<IntPtr>));
                });

            return ref Unsafe.As<GetRef<T>>(getter)();
        }

        public static ref TValue FieldRef<TSource, TValue>(ref TSource source, string fieldName)
        {
            IntermediateGetRef<TSource> getter = (IntermediateGetRef<TSource>)s_delegateCache.GetOrAdd(
                new Key(typeof(TSource), fieldName),
                static key =>
                {
                    DynamicMethod method = new(
                        "GetFieldRef",
                        typeof(IntPtr),
                        new[] { key.Type.MakeByRefType() },
                        typeof(Reflect).Module,
                        skipVisibility: true);

                    ILGenerator il = method.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    if (!typeof(TSource).IsValueType)
                    {
                        il.Emit(OpCodes.Ldind_Ref);
                    }

                    il.Emit(OpCodes.Ldflda, key.Type.GetField(key.Name, BindingFlags.Instance | BindingFlags.NonPublic));
                    il.Emit(OpCodes.Ret);
                    return method.CreateDelegate(typeof(IntermediateGetRef<TSource>));
                });

            return ref Unsafe.As<GetRef<TSource, TValue>>(getter)(ref source);
        }
    }
}
