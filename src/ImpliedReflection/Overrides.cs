using System;
using System.Linq.Expressions;
using System.Management.Automation;

using static System.Linq.Expressions.Expression;

namespace ImpliedReflection
{
    internal static class Overrides<TMemberType>
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
                    if (s_getMember != null)
                    {
                        return s_getMember;
                    }

                    return s_getMember = CreateGetMemberDelegate();
                }
            }
        }

        public static MulticastDelegate GetMembers
        {
            get
            {
                lock (s_syncLock)
                {
                    if (s_getMembers != null)
                    {
                        return s_getMembers;
                    }

                    return s_getMembers = CreateGetMembersDelegate();
                }
            }
        }

        private static MulticastDelegate CreateGetMemberDelegate()
        {
            LabelTarget returnLabel = Label(typeof(TMemberType));
            ParameterExpression obj = Parameter(typeof(PSObject), nameof(obj));
            ParameterExpression memberName = Parameter(typeof(string), nameof(memberName));

            // Creates an expression similar to:
            // UseMemberTable(memberTable => memberTable.Members[memberName]);
            return (MulticastDelegate)Lambda(
                Generics<TMemberType>.GetMember,
                UseMemberTable(
                    obj,
                    memberTable => MakeIndex(
                        Property(memberTable, GetCollectionPropertyName()),
                        typeof(Members<TMemberType>).GetProperty("Item"),
                        new[] { memberName })),
                new[] { obj, memberName })
                .Compile();
        }

        private static MulticastDelegate CreateGetMembersDelegate()
        {
            LabelTarget returnLabel = Label(Generics<TMemberType>.InternalCollection);
            ParameterExpression obj = Parameter(typeof(PSObject), nameof(obj));
            ParameterExpression resultCollection = Variable(Generics<TMemberType>.InternalCollection);

            // Creates an expression similar to:
            //      var resultCollection = new PSMemberInfoInternalCollection<TMemberType>();
            //      UseMemberTable(
            //          memberTable =>
            //          {
            //              foreach (TMemberType member in memberTable.Members)
            //              {
            //                  resultCollection.Add(member);
            //              }
            //          });
            //      return resultCollection;
            return (MulticastDelegate)Lambda(
                Generics<TMemberType>.GetMembers,
                Block(
                    new[] { resultCollection },
                    Assign(resultCollection, New(Generics<TMemberType>.InternalCollection)),
                    UseMemberTable(
                        obj,
                        memberTable => Expr.ForEach<TMemberType>(
                            Property(memberTable, GetCollectionPropertyName()),
                            (current, @break, @continue) => Call(
                                resultCollection,
                                Generics<TMemberType>.InternalCollection_Add,
                                current,
                                Expr.True))),
                    resultCollection),
                new[] { obj })
                .Compile();
        }

        private static Expression UseMemberTable(
            ParameterExpression pso,
            Func<Expression, Expression> user)
        {
            ParameterExpression memberTable = Variable(typeof(PSMemberData));
            // Creates an expression similar to:
            //      PSMemberData memberTable = PSMemberData.Get(pso.BaseObject.GetType(), pso.BaseObject);
            //      user(memberTable);
            return Block(
                new[] { memberTable },
                Assign(
                    memberTable,
                    Call(
                        ((Func<Type, object, PSMemberData>)PSMemberData.Get).Method,
                        Expr.GetRuntimeType(Expr.Base(pso)),
                        Expr.Base(pso))),
                user(memberTable));
        }

        private static string GetCollectionPropertyName()
        {
            if (typeof(TMemberType) == typeof(PSMethodInfo))
            {
                return nameof(PSMemberData.Methods);
            }

            if (typeof(TMemberType) == typeof(PSPropertyInfo))
            {
                return nameof(PSMemberData.Properties);
            }

            return nameof(PSMemberData.Members);
        }
    }
}
