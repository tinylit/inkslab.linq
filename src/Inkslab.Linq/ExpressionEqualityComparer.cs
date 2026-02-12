using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Inkslab.Linq
{
    /// <summary>
    ///     A comparer which implements <see cref="IEqualityComparer{T}" /> for <see cref="Expression" />.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-providers">Implementation of database providers and extensions</see>
    ///     and <see href="https://aka.ms/efcore-docs-how-query-works">How EF Core queries work</see> for more information and examples.
    /// </remarks>
    public sealed class ExpressionEqualityComparer : IEqualityComparer<Expression>
    {
        /// <summary>
        ///     Creates a new <see cref="ExpressionEqualityComparer" />.
        /// </summary>
        private ExpressionEqualityComparer()
        {
        }

        /// <summary>
        ///     Gets an instance of <see cref="ExpressionEqualityComparer" />.
        /// </summary>
        public static ExpressionEqualityComparer Instance { get; } = new();

        // 预分配的比较器实例，避免每次调用 Equals 时创建新的结构体
        [ThreadStatic]
        private static ExpressionComparer _comparer;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(Expression obj)
        {
            var hash = new HashCode();
            AddExpressionToHash(ref hash, obj);
            return hash.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddExpressionToHash(ref HashCode hash, Expression obj)
        {
            hash.Add(obj.NodeType);
            hash.Add(obj.Type);

            // 使用 NodeType 进行快速分发，避免模式匹配的类型检查开销
            switch (obj.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Power:
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:
                case ExpressionType.Assign:
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.DivideAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.AndAssign:
                case ExpressionType.OrAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.RightShiftAssign:
                    HashBinary(ref hash, (BinaryExpression)obj);
                    break;

                case ExpressionType.Block:
                    HashBlock(ref hash, (BlockExpression)obj);
                    break;

                case ExpressionType.Conditional:
                    HashConditional(ref hash, (ConditionalExpression)obj);
                    break;

                case ExpressionType.Constant:
                    HashConstant(ref hash, (ConstantExpression)obj);
                    break;

                case ExpressionType.Default:
                    // Intentionally empty. No additional members
                    break;

                case ExpressionType.Goto:
                    HashGoto(ref hash, (GotoExpression)obj);
                    break;

                case ExpressionType.Index:
                    HashIndex(ref hash, (IndexExpression)obj);
                    break;

                case ExpressionType.Invoke:
                    HashInvocation(ref hash, (InvocationExpression)obj);
                    break;

                case ExpressionType.Label:
                    HashLabel(ref hash, (LabelExpression)obj);
                    break;

                case ExpressionType.Lambda:
                    HashLambda(ref hash, (LambdaExpression)obj);
                    break;

                case ExpressionType.ListInit:
                    HashListInit(ref hash, (ListInitExpression)obj);
                    break;

                case ExpressionType.Loop:
                    HashLoop(ref hash, (LoopExpression)obj);
                    break;

                case ExpressionType.MemberAccess:
                    HashMember(ref hash, (MemberExpression)obj);
                    break;

                case ExpressionType.MemberInit:
                    HashMemberInit(ref hash, (MemberInitExpression)obj);
                    break;

                case ExpressionType.Call:
                    HashMethodCall(ref hash, (MethodCallExpression)obj);
                    break;

                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
                    HashNewArray(ref hash, (NewArrayExpression)obj);
                    break;

                case ExpressionType.New:
                    HashNew(ref hash, (NewExpression)obj);
                    break;

                case ExpressionType.Parameter:
                    HashParameter(ref hash, (ParameterExpression)obj);
                    break;

                case ExpressionType.RuntimeVariables:
                    HashRuntimeVariables(ref hash, (RuntimeVariablesExpression)obj);
                    break;

                case ExpressionType.Switch:
                    HashSwitch(ref hash, (SwitchExpression)obj);
                    break;

                case ExpressionType.Try:
                    HashTry(ref hash, (TryExpression)obj);
                    break;

                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    HashTypeBinary(ref hash, (TypeBinaryExpression)obj);
                    break;

                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Unbox:
                case ExpressionType.Increment:
                case ExpressionType.Decrement:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.OnesComplement:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                case ExpressionType.Throw:
                    HashUnary(ref hash, (UnaryExpression)obj);
                    break;

                case ExpressionType.Extension:
                    hash.Add(obj);
                    break;

                default:
                    throw new NotSupportedException($"Expression type '{obj.NodeType}' is not supported.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashBinary(ref HashCode hash, BinaryExpression expr)
        {
            AddExpressionToHash(ref hash, expr.Left);
            AddExpressionToHash(ref hash, expr.Right);
            if (expr.Conversion != null)
            {
                AddExpressionToHash(ref hash, expr.Conversion);
            }

            if (expr.Method != null)
            {
                hash.Add(expr.Method);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashBlock(ref HashCode hash, BlockExpression expr)
        {
            HashExpressionList(ref hash, expr.Variables);
            HashExpressionList(ref hash, expr.Expressions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashConditional(ref HashCode hash, ConditionalExpression expr)
        {
            AddExpressionToHash(ref hash, expr.Test);
            AddExpressionToHash(ref hash, expr.IfTrue);
            AddExpressionToHash(ref hash, expr.IfFalse);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashConstant(ref HashCode hash, ConstantExpression expr)
        {
            // 对于 IQueryable 类型，哈希其内部 Expression
            // 对于非 IQueryable 类型，只按类型区分（类型已在外层添加），不哈希具体值
            if (expr.Value is IQueryable queryable)
            {
                AddExpressionToHash(ref hash, queryable.Expression);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashGoto(ref HashCode hash, GotoExpression expr)
        {
            if (expr.Value != null)
            {
                AddExpressionToHash(ref hash, expr.Value);
            }

            hash.Add(expr.Kind);
            hash.Add(expr.Target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashIndex(ref HashCode hash, IndexExpression expr)
        {
            if (expr.Object != null)
            {
                AddExpressionToHash(ref hash, expr.Object);
            }

            HashExpressionList(ref hash, expr.Arguments);
            if (expr.Indexer != null)
            {
                hash.Add(expr.Indexer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashInvocation(ref HashCode hash, InvocationExpression expr)
        {
            AddExpressionToHash(ref hash, expr.Expression);
            HashExpressionList(ref hash, expr.Arguments);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashLabel(ref HashCode hash, LabelExpression expr)
        {
            if (expr.DefaultValue != null)
            {
                AddExpressionToHash(ref hash, expr.DefaultValue);
            }

            hash.Add(expr.Target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashLambda(ref HashCode hash, LambdaExpression expr)
        {
            AddExpressionToHash(ref hash, expr.Body);
            HashExpressionList(ref hash, expr.Parameters);
            hash.Add(expr.ReturnType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashListInit(ref HashCode hash, ListInitExpression expr)
        {
            AddExpressionToHash(ref hash, expr.NewExpression);
            HashInitializerList(ref hash, expr.Initializers);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashLoop(ref HashCode hash, LoopExpression expr)
        {
            AddExpressionToHash(ref hash, expr.Body);
            if (expr.BreakLabel != null)
            {
                hash.Add(expr.BreakLabel);
            }

            if (expr.ContinueLabel != null)
            {
                hash.Add(expr.ContinueLabel);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashMember(ref HashCode hash, MemberExpression expr)
        {
            if (expr.Expression != null)
            {
                AddExpressionToHash(ref hash, expr.Expression);
            }

            hash.Add(expr.Member);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashMemberInit(ref HashCode hash, MemberInitExpression expr)
        {
            AddExpressionToHash(ref hash, expr.NewExpression);
            HashMemberBindingList(ref hash, expr.Bindings);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashMethodCall(ref HashCode hash, MethodCallExpression expr)
        {
            if (expr.Object != null)
            {
                AddExpressionToHash(ref hash, expr.Object);
            }

            HashExpressionList(ref hash, expr.Arguments);
            hash.Add(expr.Method);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashNewArray(ref HashCode hash, NewArrayExpression expr)
        {
            HashExpressionList(ref hash, expr.Expressions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashNew(ref HashCode hash, NewExpression expr)
        {
            HashExpressionList(ref hash, expr.Arguments);
            if (expr.Constructor != null)
            {
                hash.Add(expr.Constructor);
            }

            var members = expr.Members;
            if (members != null)
            {
                for (int i = 0, n = members.Count; i < n; i++)
                {
                    hash.Add(members[i]);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HashParameter(ref HashCode hash, ParameterExpression expr)
        {
            if (expr.Name != null)
            {
                hash.Add(expr.Name);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashRuntimeVariables(ref HashCode hash, RuntimeVariablesExpression expr)
        {
            HashExpressionList(ref hash, expr.Variables);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashSwitch(ref HashCode hash, SwitchExpression expr)
        {
            AddExpressionToHash(ref hash, expr.SwitchValue);
            if (expr.DefaultBody != null)
            {
                AddExpressionToHash(ref hash, expr.DefaultBody);
            }

            if (expr.Comparison != null)
            {
                hash.Add(expr.Comparison);
            }

            var cases = expr.Cases;
            for (int i = 0, n = cases.Count; i < n; i++)
            {
                var @case = cases[i];
                AddExpressionToHash(ref hash, @case.Body);
                HashExpressionList(ref hash, @case.TestValues);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashTry(ref HashCode hash, TryExpression expr)
        {
            AddExpressionToHash(ref hash, expr.Body);
            if (expr.Fault != null)
            {
                AddExpressionToHash(ref hash, expr.Fault);
            }

            if (expr.Finally != null)
            {
                AddExpressionToHash(ref hash, expr.Finally);
            }

            var handlers = expr.Handlers;
            for (int i = 0, n = handlers.Count; i < n; i++)
            {
                var handler = handlers[i];
                AddExpressionToHash(ref hash, handler.Body);
                if (handler.Variable != null)
                {
                    AddExpressionToHash(ref hash, handler.Variable);
                }

                if (handler.Filter != null)
                {
                    AddExpressionToHash(ref hash, handler.Filter);
                }

                hash.Add(handler.Test);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashTypeBinary(ref HashCode hash, TypeBinaryExpression expr)
        {
            AddExpressionToHash(ref hash, expr.Expression);
            hash.Add(expr.TypeOperand);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashUnary(ref HashCode hash, UnaryExpression expr)
        {
            AddExpressionToHash(ref hash, expr.Operand);
            if (expr.Method != null)
            {
                hash.Add(expr.Method);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashExpressionList<T>(ref HashCode hash, IReadOnlyList<T> expressions) where T : Expression
        {
            for (int i = 0, n = expressions.Count; i < n; i++)
            {
                AddExpressionToHash(ref hash, expressions[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashInitializerList(ref HashCode hash, IReadOnlyList<ElementInit> initializers)
        {
            for (int i = 0, n = initializers.Count; i < n; i++)
            {
                var init = initializers[i];
                HashExpressionList(ref hash, init.Arguments);
                hash.Add(init.AddMethod);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashMemberBindingList(ref HashCode hash, IReadOnlyList<MemberBinding> bindings)
        {
            for (int i = 0, n = bindings.Count; i < n; i++)
            {
                var binding = bindings[i];
                hash.Add(binding.Member);
                hash.Add(binding.BindingType);

                switch (binding)
                {
                    case MemberAssignment memberAssignment:
                        AddExpressionToHash(ref hash, memberAssignment.Expression);
                        break;
                    case MemberListBinding memberListBinding:
                        HashInitializerList(ref hash, memberListBinding.Initializers);
                        break;
                    case MemberMemberBinding memberMemberBinding:
                        HashMemberBindingList(ref hash, memberMemberBinding.Bindings);
                        break;
                }
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Expression x, Expression y)
        {
            // 使用 ThreadStatic 缓存的比较器实例，避免每次创建新结构体
            _comparer ??= new ExpressionComparer();
            return _comparer.Compare(x, y);
        }

        private sealed class ExpressionComparer
        {
            // 使用列表替代字典，对于典型的浅层 Lambda 更高效
            private List<(ParameterExpression Left, ParameterExpression Right)> _parameterScope;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Compare(Expression left, Expression right)
            {
                if (ReferenceEquals(left, right))
                {
                    return true;
                }

                if (left is null || right is null)
                {
                    return false;
                }

                if (left.NodeType != right.NodeType)
                {
                    return false;
                }

                if (left.Type != right.Type)
                {
                    return false;
                }

                // 使用 NodeType 预分发，避免模式匹配的类型检查开销
                return left.NodeType switch
                {
                    ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.Subtract
                        or ExpressionType.SubtractChecked or ExpressionType.Multiply
                        or ExpressionType.MultiplyChecked or ExpressionType.Divide
                        or ExpressionType.Modulo or ExpressionType.Power or ExpressionType.And
                        or ExpressionType.Or or ExpressionType.AndAlso or ExpressionType.OrElse
                        or ExpressionType.LessThan or ExpressionType.LessThanOrEqual
                        or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
                        or ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.Coalesce
                        or ExpressionType.ArrayIndex or ExpressionType.ExclusiveOr
                        or ExpressionType.LeftShift or ExpressionType.RightShift
                        or ExpressionType.Assign or ExpressionType.AddAssign
                        or ExpressionType.AddAssignChecked or ExpressionType.SubtractAssign
                        or ExpressionType.SubtractAssignChecked or ExpressionType.MultiplyAssign
                        or ExpressionType.MultiplyAssignChecked or ExpressionType.DivideAssign
                        or ExpressionType.ModuloAssign or ExpressionType.PowerAssign
                        or ExpressionType.AndAssign or ExpressionType.OrAssign
                        or ExpressionType.ExclusiveOrAssign or ExpressionType.LeftShiftAssign
                        or ExpressionType.RightShiftAssign
                        => CompareBinary((BinaryExpression)left, (BinaryExpression)right),

                    ExpressionType.Block => CompareBlock((BlockExpression)left, (BlockExpression)right),
                    ExpressionType.Conditional => CompareConditional((ConditionalExpression)left, (ConditionalExpression)right),
                    ExpressionType.Constant => CompareConstant((ConstantExpression)left, (ConstantExpression)right),
                    ExpressionType.Default => true,
                    ExpressionType.Goto => CompareGoto((GotoExpression)left, (GotoExpression)right),
                    ExpressionType.Index => CompareIndex((IndexExpression)left, (IndexExpression)right),
                    ExpressionType.Invoke => CompareInvocation((InvocationExpression)left, (InvocationExpression)right),
                    ExpressionType.Label => CompareLabel((LabelExpression)left, (LabelExpression)right),
                    ExpressionType.Lambda => CompareLambda((LambdaExpression)left, (LambdaExpression)right),
                    ExpressionType.ListInit => CompareListInit((ListInitExpression)left, (ListInitExpression)right),
                    ExpressionType.Loop => CompareLoop((LoopExpression)left, (LoopExpression)right),
                    ExpressionType.MemberAccess => CompareMember((MemberExpression)left, (MemberExpression)right),
                    ExpressionType.MemberInit => CompareMemberInit((MemberInitExpression)left, (MemberInitExpression)right),
                    ExpressionType.Call => CompareMethodCall((MethodCallExpression)left, (MethodCallExpression)right),
                    ExpressionType.NewArrayBounds or ExpressionType.NewArrayInit
                        => CompareNewArray((NewArrayExpression)left, (NewArrayExpression)right),
                    ExpressionType.New => CompareNew((NewExpression)left, (NewExpression)right),
                    ExpressionType.Parameter => CompareParameter((ParameterExpression)left, (ParameterExpression)right),
                    ExpressionType.RuntimeVariables => CompareRuntimeVariables((RuntimeVariablesExpression)left, (RuntimeVariablesExpression)right),
                    ExpressionType.Switch => CompareSwitch((SwitchExpression)left, (SwitchExpression)right),
                    ExpressionType.Try => CompareTry((TryExpression)left, (TryExpression)right),
                    ExpressionType.TypeIs or ExpressionType.TypeEqual
                        => CompareTypeBinary((TypeBinaryExpression)left, (TypeBinaryExpression)right),
                    ExpressionType.Negate or ExpressionType.NegateChecked or ExpressionType.Not
                        or ExpressionType.Convert or ExpressionType.ConvertChecked
                        or ExpressionType.ArrayLength or ExpressionType.Quote
                        or ExpressionType.TypeAs or ExpressionType.UnaryPlus
                        or ExpressionType.Unbox or ExpressionType.Increment
                        or ExpressionType.Decrement or ExpressionType.PreIncrementAssign
                        or ExpressionType.PostIncrementAssign or ExpressionType.PreDecrementAssign
                        or ExpressionType.PostDecrementAssign or ExpressionType.OnesComplement
                        or ExpressionType.IsTrue or ExpressionType.IsFalse or ExpressionType.Throw
                        => CompareUnary((UnaryExpression)left, (UnaryExpression)right),

                    ExpressionType.Extension => left.Equals(right),
                    _ => throw new InvalidOperationException($"Expression type '{left.NodeType}' is not supported.")
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareBinary(BinaryExpression a, BinaryExpression b)
                => a.Method == b.Method
                    && a.IsLifted == b.IsLifted
                    && a.IsLiftedToNull == b.IsLiftedToNull
                    && Compare(a.Left, b.Left)
                    && Compare(a.Right, b.Right)
                    && Compare(a.Conversion, b.Conversion);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareBlock(BlockExpression a, BlockExpression b)
                => CompareExpressionList(a.Variables, b.Variables)
                    && CompareExpressionList(a.Expressions, b.Expressions);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareConditional(ConditionalExpression a, ConditionalExpression b)
                => Compare(a.Test, b.Test)
                    && Compare(a.IfTrue, b.IfTrue)
                    && Compare(a.IfFalse, b.IfFalse);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareConstant(ConstantExpression a, ConstantExpression b)
            {
                var (v1, v2) = (a.Value, b.Value);

                // 先进行引用相等检查（更快）
                if (ReferenceEquals(v1, v2))
                {
                    return true;
                }

                // 对于 IQueryable 类型，比较其内部 Expression
                if (v1 is IQueryable q1 && v2 is IQueryable q2)
                {
                    return Compare(q1.Expression, q2.Expression);
                }

                // 非 IQueryable 类型：类型相同即视为相同（类型已在外层比较过）
                // 这允许参数化查询缓存命中，即使常量值不同
                return v1 is not IQueryable && v2 is not IQueryable;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareGoto(GotoExpression a, GotoExpression b)
                => a.Kind == b.Kind
                    && a.Target == b.Target
                    && Compare(a.Value, b.Value);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareIndex(IndexExpression a, IndexExpression b)
                => a.Indexer == b.Indexer
                    && Compare(a.Object, b.Object)
                    && CompareExpressionList(a.Arguments, b.Arguments);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareInvocation(InvocationExpression a, InvocationExpression b)
                => Compare(a.Expression, b.Expression)
                    && CompareExpressionList(a.Arguments, b.Arguments);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareLabel(LabelExpression a, LabelExpression b)
                => a.Target == b.Target
                    && Compare(a.DefaultValue, b.DefaultValue);

            private bool CompareLambda(LambdaExpression a, LambdaExpression b)
            {
                var n = a.Parameters.Count;

                if (b.Parameters.Count != n)
                {
                    return false;
                }

                // 使用列表作为栈，对于浅层嵌套更高效
                _parameterScope ??= new List<(ParameterExpression, ParameterExpression)>();
                var scopeStartIndex = _parameterScope.Count;

                for (var i = 0; i < n; i++)
                {
                    var (p1, p2) = (a.Parameters[i], b.Parameters[i]);

                    if (p1.Type != p2.Type)
                    {
                        // 清理已添加的参数
                        _parameterScope.RemoveRange(scopeStartIndex, _parameterScope.Count - scopeStartIndex);
                        return false;
                    }

                    _parameterScope.Add((p1, p2));
                }

                try
                {
                    return Compare(a.Body, b.Body);
                }
                finally
                {
                    // 只移除当前作用域添加的参数
                    _parameterScope.RemoveRange(scopeStartIndex, n);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareListInit(ListInitExpression a, ListInitExpression b)
                => Compare(a.NewExpression, b.NewExpression)
                    && CompareElementInitList(a.Initializers, b.Initializers);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareLoop(LoopExpression a, LoopExpression b)
                => a.BreakLabel == b.BreakLabel
                    && a.ContinueLabel == b.ContinueLabel
                    && Compare(a.Body, b.Body);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareMember(MemberExpression a, MemberExpression b)
                => a.Member == b.Member
                    && Compare(a.Expression, b.Expression);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareMemberInit(MemberInitExpression a, MemberInitExpression b)
                => Compare(a.NewExpression, b.NewExpression)
                    && CompareMemberBindingList(a.Bindings, b.Bindings);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareMethodCall(MethodCallExpression a, MethodCallExpression b)
                => a.Method == b.Method
                    && Compare(a.Object, b.Object)
                    && CompareExpressionList(a.Arguments, b.Arguments);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareNewArray(NewArrayExpression a, NewArrayExpression b)
                => CompareExpressionList(a.Expressions, b.Expressions);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareNew(NewExpression a, NewExpression b)
                => a.Constructor == b.Constructor
                    && CompareExpressionList(a.Arguments, b.Arguments)
                    && CompareMemberList(a.Members, b.Members);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareParameter(ParameterExpression a, ParameterExpression b)
            {
                // 从列表末尾向前搜索（最近添加的参数最可能匹配）
                if (_parameterScope != null)
                {
                    for (int i = _parameterScope.Count - 1; i >= 0; i--)
                    {
                        var (left, right) = _parameterScope[i];
                        if (ReferenceEquals(left, a))
                        {
                            return right.Name == b.Name;
                        }
                    }
                }
                return a.Name == b.Name;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareRuntimeVariables(RuntimeVariablesExpression a, RuntimeVariablesExpression b)
                => CompareExpressionList(a.Variables, b.Variables);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareSwitch(SwitchExpression a, SwitchExpression b)
                => a.Comparison == b.Comparison
                    && Compare(a.SwitchValue, b.SwitchValue)
                    && Compare(a.DefaultBody, b.DefaultBody)
                    && CompareSwitchCaseList(a.Cases, b.Cases);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareTry(TryExpression a, TryExpression b)
                => Compare(a.Body, b.Body)
                    && Compare(a.Fault, b.Fault)
                    && Compare(a.Finally, b.Finally)
                    && CompareCatchBlockList(a.Handlers, b.Handlers);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareTypeBinary(TypeBinaryExpression a, TypeBinaryExpression b)
                => a.TypeOperand == b.TypeOperand
                    && Compare(a.Expression, b.Expression);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareUnary(UnaryExpression a, UnaryExpression b)
                => a.Method == b.Method
                    && a.IsLifted == b.IsLifted
                    && a.IsLiftedToNull == b.IsLiftedToNull
                    && Compare(a.Operand, b.Operand);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareExpressionList(IReadOnlyList<Expression> a, IReadOnlyList<Expression> b)
            {
                if (ReferenceEquals(a, b))
                {
                    return true;
                }

                if (a is null || b is null)
                {
                    return false;
                }

                var count = a.Count;
                if (count != b.Count)
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!Compare(a[i], b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool CompareMemberList(IReadOnlyList<MemberInfo> a, IReadOnlyList<MemberInfo> b)
            {
                if (ReferenceEquals(a, b))
                {
                    return true;
                }

                if (a is null || b is null)
                {
                    return false;
                }

                var count = a.Count;
                if (count != b.Count)
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (a[i] != b[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool CompareMemberBindingList(IReadOnlyList<MemberBinding> a, IReadOnlyList<MemberBinding> b)
            {
                if (ReferenceEquals(a, b))
                {
                    return true;
                }

                if (a is null || b is null)
                {
                    return false;
                }

                var count = a.Count;
                if (count != b.Count)
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!CompareBinding(a[i], b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareBinding(MemberBinding a, MemberBinding b)
            {
                if (ReferenceEquals(a, b))
                {
                    return true;
                }

                if (a is null || b is null)
                {
                    return false;
                }

                if (a.BindingType != b.BindingType)
                {
                    return false;
                }

                if (a.Member != b.Member)
                {
                    return false;
                }

                return a switch
                {
                    MemberAssignment aMemberAssignment => Compare(aMemberAssignment.Expression, ((MemberAssignment)b).Expression),
                    MemberListBinding aMemberListBinding => CompareElementInitList(aMemberListBinding.Initializers, ((MemberListBinding)b).Initializers),
                    MemberMemberBinding aMemberMemberBinding => CompareMemberBindingList(aMemberMemberBinding.Bindings, ((MemberMemberBinding)b).Bindings),
                    _ => throw new InvalidOperationException()
                };
            }

            private bool CompareElementInitList(IReadOnlyList<ElementInit> a, IReadOnlyList<ElementInit> b)
            {
                if (ReferenceEquals(a, b))
                {
                    return true;
                }

                if (a is null || b is null)
                {
                    return false;
                }

                var count = a.Count;
                if (count != b.Count)
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!CompareElementInit(a[i], b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareElementInit(ElementInit a, ElementInit b)
                => a.AddMethod == b.AddMethod
                    && CompareExpressionList(a.Arguments, b.Arguments);

            private bool CompareSwitchCaseList(IReadOnlyList<SwitchCase> a, IReadOnlyList<SwitchCase> b)
            {
                if (ReferenceEquals(a, b))
                {
                    return true;
                }

                if (a is null || b is null)
                {
                    return false;
                }

                var count = a.Count;
                if (count != b.Count)
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!CompareSwitchCase(a[i], b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareSwitchCase(SwitchCase a, SwitchCase b)
                => Compare(a.Body, b.Body)
                    && CompareExpressionList(a.TestValues, b.TestValues);

            private bool CompareCatchBlockList(IReadOnlyList<CatchBlock> a, IReadOnlyList<CatchBlock> b)
            {
                if (ReferenceEquals(a, b))
                {
                    return true;
                }

                if (a is null || b is null)
                {
                    return false;
                }

                var count = a.Count;
                if (count != b.Count)
                {
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!CompareCatchBlock(a[i], b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool CompareCatchBlock(CatchBlock a, CatchBlock b)
                => a.Test == b.Test
                   && Compare(a.Body, b.Body)
                   && Compare(a.Filter, b.Filter)
                   && Compare(a.Variable, b.Variable);
        }
    }
}