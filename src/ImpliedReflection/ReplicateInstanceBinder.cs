using System;
using System.Dynamic;
using System.Reflection;

using BindFlags = ImpliedReflection.Bind;

using static System.Linq.Expressions.Expression;
using System.Runtime.CompilerServices;
using System.Management.Automation;
using System.Linq.Expressions;

namespace ImpliedReflection
{
    internal class ReplicateInstanceBinder : InvokeMemberBinder
    {
        public readonly static ReplicateInstanceBinder Instance = new ReplicateInstanceBinder();

        private ReplicateInstanceBinder()
            : base(nameof(PSMemberInfoExtensions.ReplicateInstance), ignoreCase: false, new CallInfo(1, "instance"))
        {
        }

        public override DynamicMetaObject FallbackInvoke(
            DynamicMetaObject target,
            DynamicMetaObject[] args,
            DynamicMetaObject errorSuggestion)
        {
            throw new System.NotImplementedException();
        }

        public override DynamicMetaObject FallbackInvokeMember(
            DynamicMetaObject target,
            DynamicMetaObject[] args,
            DynamicMetaObject errorSuggestion)
        {
            Type type = target.RuntimeType;
            MethodInfo method = type.GetMethod(
                VersionSpecificMemberNames.PrivateMemberNames.PSMemberInfo_ReplicateInstance,
                BindFlags.Any.Instance,
                null,
                new[] { typeof(object) },
                null);

            if (method == null)
            {
                return new DynamicMetaObject(
                    Block(
                        Throw(New(Cache.RuntimeException_ctor, Constant(ImpliedReflectionStrings.InvalidMemberInfo))),
                        Default(typeof(object))),
                    BindingRestrictions.GetTypeRestriction(target.Expression, type));
            }

            Expression instance = Convert(target.Expression, type);
            Expression callExpr = Call(instance, method, args[0].Expression);

            if (typeof(PSProperty).IsAssignableFrom(type))
            {
                // PSProperty doesn't override ReplicateInstance, so the call doesn't actually change
                // it's private baseObject field.
                FieldInfo baseObject = typeof(PSProperty).GetField(
                    nameof(baseObject),
                    BindFlags.NonPublic.Instance);

                return new DynamicMetaObject(
                    Block(callExpr, Assign(Field(instance, baseObject), args[0].Expression), Default(typeof(object))),
                    BindingRestrictions.GetTypeRestriction(target.Expression, type));
            }

            return new DynamicMetaObject(
                Block(callExpr, Default(typeof(object))),
                BindingRestrictions.GetTypeRestriction(target.Expression, type));
        }
    }
}
