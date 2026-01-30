using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// <see cref="Queryable.Select{TSource, TResult}(IQueryable{TSource}, System.Linq.Expressions.Expression{System.Func{TSource,TResult}})"/>。
    /// </summary>
    [DebuggerDisplay("SelectField")]
    public class SelectListVisitor : BaseVisitor
    {
        private readonly bool _showAs;
        private readonly bool _isGroupHaving;

        /// <inheritdoc/>
        public SelectListVisitor(CoreVisitor visitor, bool showAs = false, bool isGroupHaving = false) : base(visitor)
        {
            _showAs = showAs;
            _isGroupHaving = isGroupHaving;
        }

        /// <inheritdoc/>
        protected override void PreparingParameter(LambdaExpression node)
        {
            //? 查询字段不准备参数。
        }

        /// <inheritdoc/>
        protected override void Condition(Expression node)
        {
            using (var visitor = new ConditionVisitor(this, _isGroupHaving))
            {
                visitor.Startup(node);
            }
        }

        /// <inheritdoc/>
        protected override void Member(MemberInfo memberInfo, Expression node)
        {
            using (var visitor = new AsVisitor(this))
            {
                visitor.Startup(node);
            }

            if (_showAs)
            {
                Writer.AsName(memberInfo.Name);
            }
        }

        /// <inheritdoc/>
        protected override void Member(string schema, string field, string name)
        {
            base.Member(schema, field, name);

            if (_showAs)
            {
                Writer.AsName(name);
            }
        }

        /// <inheritdoc/>
        protected override void Constant(IQueryable value)
        {
            bool commaFlag = false;

            var tableInformation = Table();

            foreach (var (name, field) in tableInformation.Fields)
            {
                if (commaFlag)
                {
                    Writer.Delimiter();
                }
                else
                {
                    commaFlag = true;
                }

                Member(string.Empty, field, name);
            }
        }
    }
}
