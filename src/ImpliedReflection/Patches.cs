using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using static System.Management.Automation.DotNetAdapter;

namespace ImpliedReflection;

internal sealed class Patches : IDisposable
{
    private readonly PatchHandle[] _patches;

    private bool _isDisposed;

    private Patches(params PatchHandle[] patches) => _patches = patches;

    public static Patches Create()
    {
            var harmony = new Harmony("ImpliedReflection");
            return new Patches(
                PatchHandle.Create(
                    harmony,
                    typeof(PSCreateInstanceBinder)
                        .GetMethod(
                            nameof(PSCreateInstanceBinder.FallbackCreateInstance),
                            BindTo.Public.Instance,
                            binder: null,
                            new[] { typeof(DynamicMetaObject), typeof(DynamicMetaObject[]), typeof(DynamicMetaObject) },
                            modifiers: null),
                    prefix: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(
                                nameof(Patches.FallbackCreateInstancePrefix),
                                BindTo.Public.Static))),
                PatchHandle.Create(
                    harmony,
                    typeof(PropertyCacheEntry)
                        .GetConstructor(
                            BindTo.NonPublic.Instance,
                            binder: null,
                            new[] { typeof(PropertyInfo) },
                            modifiers: null),
                    postfix: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(
                                nameof(Patches.PropertyCacheEntryPropertyCtorPostFix),
                                BindTo.Public.Static))),
                PatchHandle.Create(
                    harmony,
                    typeof(PropertyCacheEntry)
                        .GetConstructor(
                            BindTo.NonPublic.Instance,
                            binder: null,
                            new[] { typeof(FieldInfo) },
                            modifiers: null),
                    postfix: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(
                                nameof(Patches.PropertyCacheEntryFieldCtorPostFix),
                                BindTo.Public.Static))),
                PatchHandle.Create(
                    harmony,
                    typeof(PSGetMemberBinder)
                        .GetMethod(
                            nameof(PSGetMemberBinder.FallbackGetMember),
                            BindTo.Public.Instance,
                            binder: null,
                            new[] { typeof(DynamicMetaObject), typeof(DynamicMetaObject) },
                            modifiers: null),
                    transpiler: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(nameof(FallbackGetMemberTranspiler), BindTo.Public.Static))),
                PatchHandle.Create(
                    harmony,
                    typeof(TypeResolver)
                        .GetMethod(
                            nameof(TypeResolver.IsPublic),
                            BindTo.NonPublic.Static,
                            binder: null,
                            new[] { typeof(Type) },
                            modifiers: null),
                    prefix: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(nameof(IsPublicPrefix), BindTo.NonPublic.Static))),
#if PS5_1
                PatchHandle.Create(
                    harmony,
                    typeof(TypeResolver)
                        .GetMethod(
                            nameof(TypeResolver.IsPublic),
                            BindTo.NonPublic.Static,
                            binder: null,
                            new[] { typeof(TypeInfo) },
                            modifiers: null),
                    prefix: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(nameof(IsPublic2Prefix), BindTo.NonPublic.Static))),
#endif
                PatchHandle.Create(
                    harmony,
                    typeof(DotNetAdapter)
                        .GetMethod(
                            nameof(DotNetAdapter.GetPropertiesAndMethods),
                            BindTo.NonPublic.Instance,
                            binder: null,
                            new[] { typeof(Type), typeof(bool) },
                            modifiers: null)
                        .Prepare(),
                    prefix: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(nameof(GetPropertiesAndMethodsPrefix), BindTo.NonPublic.Static))),
                PatchHandle.Create(
                    harmony,
                    typeof(DotNetAdapter)
                        .GetMethod(
                            nameof(NonPublicAdapter.PopulatePropertyReflectionTable),
                            BindTo.NonPublic.Static,
                            binder: null,
                            new[] { typeof(Type), typeof(CacheTable), typeof(BindingFlags) },
                            modifiers: null)
                        .Prepare(),
                    prefix: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(nameof(PopulatePropertyReflectionTablePrefix), BindTo.NonPublic.Static))),
                PatchHandle.Create(
                    harmony,
                    typeof(DotNetAdapter)
                        .GetMethod(
                            nameof(NonPublicAdapter.PopulateMethodReflectionTable),
                            BindTo.NonPublic.Static,
                            binder: null,
                            new[] { typeof(Type), typeof(CacheTable), typeof(BindingFlags) },
                            modifiers: null)
                        .Prepare(),
                    prefix: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(nameof(PopulateMethodReflectionTablePrefix), BindTo.NonPublic.Static))),
                PatchHandle.Create(
                    harmony,
                    typeof(DotNetAdapter)
                        .GetMethod(
                            nameof(NonPublicAdapter.PopulateEventReflectionTable),
                            BindTo.NonPublic.Static,
                            binder: null,
                            new[] { typeof(Type), typeof(Dictionary<string, EventCacheEntry>), typeof(BindingFlags) },
                            modifiers: null)
                        .Prepare(),
                    prefix: new HarmonyMethod(
                        typeof(Patches)
                            .GetMethod(nameof(PopulateEventReflectionTablePrefix), BindTo.NonPublic.Static))));
    }

    public static bool FallbackCreateInstancePrefix(
        PSCreateInstanceBinder __instance,
        DynamicMetaObject target,
        DynamicMetaObject[] args,
        DynamicMetaObject errorSuggestion,
        ref DynamicMetaObject __result)
    {
        if (!target.HasValue || args.Any(arg => !arg.HasValue))
        {
            __result = __instance.Defer(args.Prepend(target).ToArray());
            return false;
        }

        var targetValue = PSObject.Base(target.Value);
        if (targetValue == null)
        {
            __result = target.ThrowRuntimeError(
                args,
                BindingRestrictions.Empty,
                "InvokeMethodOnNull",
                ParserStrings.InvokeMethodOnNull)
                .WriteToDebugLog(__instance);

            return false;
        }

        var instanceType = targetValue as Type ?? targetValue.GetType();

        BindingRestrictions restrictions;
#if !PS5_1
        if (instanceType.GetIsByRefLike())
        {
            // ByRef-like types are not boxable and should be used only on stack
            restrictions = BindingRestrictions.GetExpressionRestriction(
                Expression.Call(CachedReflectionInfo.PSCreateInstanceBinder_IsTargetTypeByRefLike, target.Expression));

            __result = target.ThrowRuntimeError(
                restrictions,
                nameof(ExtendedTypeSystem.CannotInstantiateBoxedByRefLikeType),
                ExtendedTypeSystem.CannotInstantiateBoxedByRefLikeType,
                Expression.Call(
                    CachedReflectionInfo.PSCreateInstanceBinder_GetTargetTypeName,
                    target.Expression))
                    .WriteToDebugLog(__instance);

            return false;
        }
#endif

        var ctors = instanceType.GetConstructors(BindTo.Any.Instance);
        restrictions = ReferenceEquals(instanceType, targetValue)
                            ? (target.Value is PSObject)
                                    ? BindingRestrictions.GetInstanceRestriction(Expression.Call(CachedReflectionInfo.PSObject_Base, target.Expression), instanceType)
                                    : BindingRestrictions.GetInstanceRestriction(target.Expression, instanceType)
                            : target.PSGetTypeRestriction();
        restrictions = restrictions.Merge(
            BinderUtils.GetOptionalVersionAndLanguageCheckForType(
                __instance,
                instanceType,
                __instance._version()));

        // The check in SMA here is actually "if no constructors are defined and
        // zero args are specified, then get default". Here I change it to be
        // "if zero args are specified, and there is no constructor that takes
        // zero args".
        if (__instance._callInfo().ArgumentCount is 0 && instanceType.IsValueType)
        {
            ConstructorInfo targetCtor = instanceType.GetConstructor(
                BindTo.Any.Instance,
                binder: null,
                Type.EmptyTypes,
                modifiers: null);

            if (targetCtor is null)
            {
                __result = new DynamicMetaObject(
                    Expression.New(instanceType)
                        .Cast(__instance.ReturnType), restrictions)
                        .WriteToDebugLog(__instance);

                return false;
            }
        }

        var context = LocalPipeline.GetExecutionContextFromTLS();
        if (context != null
            && context.LanguageMode == PSLanguageMode.ConstrainedLanguage
            && !CoreTypes.Contains(instanceType))
        {
            __result = target.ThrowRuntimeError(
                restrictions,
                "CannotCreateTypeConstrainedLanguage",
                ParserStrings.CannotCreateTypeConstrainedLanguage)
                .WriteToDebugLog(__instance);

            return false;
        }

        restrictions = args.Aggregate(restrictions, (current, arg) => current.Merge(arg.PSGetMethodArgumentRestriction()));
        var newConstructors = DotNetAdapter.GetMethodInformationArray(ctors);
        __result = PSInvokeMemberBinder.InvokeDotNetMethod(
            __instance._callInfo(),
            "new",
            __instance._constraints(),
            PSInvokeMemberBinder.MethodInvocationType.Ordinary,
            target,
            args,
            restrictions,
            newConstructors,
            typeof(MethodException))
            .WriteToDebugLog(__instance);

        return false;
    }

    public static void PropertyCacheEntryPropertyCtorPostFix(PropertyCacheEntry __instance, PropertyInfo property)
    {
        __instance.readOnly = property.GetSetMethod(true) is null;
        __instance.writeOnly = property.GetGetMethod(true) is null;
    }

    public static void PropertyCacheEntryFieldCtorPostFix(PropertyCacheEntry __instance, FieldInfo field)
    {
        __instance.readOnly = field.IsLiteral;
    }

    public static IEnumerable<CodeInstruction> FallbackGetMemberTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo getIsFamily = typeof(MethodBase)
            .GetProperty("IsFamily")
            .GetGetMethod();

        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(getIsFamily))
            {
                yield return new CodeInstruction(OpCodes.Pop);
                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                continue;
            }

            yield return instruction;
        }
    }

    private static bool IsPublicPrefix(Type type, ref bool __result)
    {
        __result = true;
        return false;
    }

    private static bool IsPublic2Prefix(TypeInfo typeInfo, ref bool __result)
    {
        __result = true;
        return false;
    }

    private static void GetPropertiesAndMethodsPrefix(Type type, bool @static)
    {
        if (@static)
        {
            NonPublicAdapter.GetStaticMethodReflectionTable(type);
            NonPublicAdapter.GetStaticPropertyReflectionTable(type);
            return;
        }

        NonPublicAdapter.GetInstanceMethodReflectionTable(type);
        NonPublicAdapter.GetInstancePropertyReflectionTable(type);
    }

    private static bool PopulatePropertyReflectionTablePrefix(Type type, CacheTable typeProperties, BindingFlags bindingFlags)
    {
        bindingFlags |= BindingFlags.NonPublic;
        NonPublicAdapter.PopulatePropertyReflectionTable(type, typeProperties, bindingFlags);
        return false;
    }

    private static bool PopulateMethodReflectionTablePrefix(Type type, CacheTable typeMethods, BindingFlags bindingFlags)
    {
        bindingFlags |= BindingFlags.NonPublic;
        NonPublicAdapter.PopulateMethodReflectionTable(type, typeMethods, bindingFlags);
        return false;
    }

    private static bool PopulateEventReflectionTablePrefix(Type type, Dictionary<string, EventCacheEntry> typeEvents, BindingFlags bindingFlags)
    {
        bindingFlags |= BindingFlags.NonPublic;
        NonPublicAdapter.PopulateEventReflectionTable(type, typeEvents, bindingFlags);
        return false;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (ref PatchHandle patch in _patches.AsSpan())
            {
                patch.Dispose();
            }

            _patches[0].Instance.UnpatchAll();
        }

        _isDisposed = true;
    }
}

internal readonly struct PatchHandle : IDisposable
{
    public readonly Harmony Instance;

    public readonly MethodBase Original;

    public readonly MethodInfo Patch;

    public PatchHandle(Harmony instance, MethodBase original, MethodInfo patch)
    {
        Instance = instance;
        Original = original;
        Patch = patch;
    }

    public static PatchHandle Create(
        Harmony instance,
        MethodBase original,
        HarmonyMethod prefix = null,
        HarmonyMethod postfix = null,
        HarmonyMethod transpiler = null,
        HarmonyMethod finalizer = null)
    {
        return new(
            instance,
            original,
            instance.Patch(original, prefix, postfix, transpiler, finalizer));
    }

    public void Dispose()
    {
        Instance.Unpatch(Original, Patch);
    }
}
