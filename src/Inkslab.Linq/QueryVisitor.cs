using System;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Exceptions;
using Inkslab.Linq.Expressions;

namespace Inkslab.Linq
{
    /// <summary>
    /// 查询访问器。
    /// </summary>
    public class QueryVisitor : SelectVisitor, IQueryVisitor
    {
        private bool userDefined;

        private string noElementError;

        private bool hasDefaultValue;

        private object defaultValue;

        private RowStyle rowStyle = RowStyle.FirstOrDefault;

        private readonly IDbAdapter _adapter;

        /// <inheritdoc/>
        public QueryVisitor(IDbAdapter adapter) : base(adapter)
        {
            _adapter = adapter;
        }

        /// <inheritdoc/>
        protected override void LinqCore(MethodCallExpression node)
        {
            var name = node.Method.Name;

            switch (name)
            {
                case nameof(Queryable.Any):
                case nameof(Queryable.All):

                    Writer.Keyword(Enums.SqlKeyword.SELECT);

                    if (_adapter.Engine == DatabaseEngine.MySQL)
                    {
                        base.LinqCore(node);

                        break;
                    }

                    Writer.Keyword(Enums.SqlKeyword.CASE);
                    Writer.Keyword(Enums.SqlKeyword.WHEN);

                    base.LinqCore(node);

                    Writer.Keyword(Enums.SqlKeyword.THEN);

                    Writer.AlwaysTrue();

                    Writer.Keyword(Enums.SqlKeyword.ELSE);

                    Writer.AlwaysFalse();

                    Writer.Keyword(Enums.SqlKeyword.END);

                    break;
                case nameof(Queryable.First):
                case nameof(Queryable.Last):
                    rowStyle = RowStyle.First;
                    goto default;
                case nameof(Queryable.FirstOrDefault):
                case nameof(Queryable.LastOrDefault):
                    rowStyle = RowStyle.FirstOrDefault;
                    goto default;
                case nameof(Queryable.Single):
                    rowStyle = RowStyle.Single;
                    goto default;
                case nameof(Queryable.SingleOrDefault):
                case nameof(Queryable.ElementAtOrDefault):
                    rowStyle = RowStyle.SingleOrDefault;
                    goto default;
                default:

                    base.LinqCore(node);

                    break;
            }
        }

        /// <inheritdoc/>
        public CommandSql<TElement> ToSQL<TElement>()
        {
            TElement elementValue = default;

            if (hasDefaultValue)
            {
                if (defaultValue is TElement value)
                {
                    elementValue = value;

                    goto label_core;
                }

                var conversionType = typeof(TElement);

                if (defaultValue is null)
                {
                    if (conversionType.IsValueType && !conversionType.IsNullable())
                    {
                        throw new DSyntaxErrorException($"默认值“null”无法转换为“{conversionType}”类型!");
                    }

                    goto label_core;
                }

                try
                {
                    if (conversionType.IsNullable())
                    {
                        elementValue = (TElement)Activator.CreateInstance(conversionType, Convert.ChangeType(defaultValue, Nullable.GetUnderlyingType(conversionType)));
                    }
                    else
                    {
                        elementValue = (TElement)Convert.ChangeType(defaultValue, conversionType);
                    }

                    goto label_core;
                }
                catch (Exception e)
                {
                    throw new DSyntaxErrorException($"查询结果类型({conversionType})和指定的默认值类型({defaultValue.GetType()})无法进行默认转换!", e);
                }

                throw new DSyntaxErrorException($"查询结果类型({conversionType})和指定的默认值类型({defaultValue.GetType()})无法进行默认转换!");
            }

        label_core:

            var commandSql = ToSQL();

            return new CommandSql<TElement>(commandSql, rowStyle, hasDefaultValue, elementValue, userDefined, noElementError);
        }

        /// <inheritdoc/>
        protected override void Backflow(ExpressionVisitor visitor, MethodCallExpression node)
        {
            var name = node.Method.Name;

            switch (name)
            {
                case nameof(QueryableExtentions.NoElementError):

                    if (rowStyle > 0 && (rowStyle & RowStyle.FirstOrDefault) == RowStyle.FirstOrDefault)
                    {
                        throw new NotSupportedException($"函数“{name}”仅在表达式链以“Min”、“Max”、“Average”、“Last”、“First”、“Single”或“ElementAt”结尾时，可用！");
                    }

                    userDefined = true;

                    noElementError = node.Arguments[1].GetValueFromExpression<string>() ?? throw new NotSupportedException($"函数“{name}”错误消息是字符串类型且不能为空！");

                    visitor.Visit(node.Arguments[0]);

                    break;
                case nameof(Queryable.DefaultIfEmpty):

                    if (hasDefaultValue)
                    {
                        throw new NotSupportedException($"函数“{name}”仅在表达式链最多只能出现一次！");
                    }

                    if (rowStyle is RowStyle.First or RowStyle.Single)
                    {
                        throw new NotSupportedException($"函数“{name}”仅在表达式链以“FirstOrDefault”、“LastOrDefault”、“SingleOrDefault”或“ElementAtOrDefault”结尾时，可用！");
                    }

                    hasDefaultValue = true;

                    defaultValue = node.Arguments[1].GetValueFromExpression();

                    visitor.Visit(node.Arguments[0]);

                    break;
                default:

                    base.Backflow(visitor, node);

                    break;
            }
        }
    }
}