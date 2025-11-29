using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 计数。
    /// </summary>
    [DebuggerDisplay("Count")]
    public class CountVisitor : ScriptVisitor
    {
        /// <summary>
        /// 生成 SELECT。
        /// </summary>
        private bool _buildSelect = true;

        /// <summary>
        /// 去重。
        /// </summary>
        private bool _isDistinct = false;


        private readonly SelectVisitor _visitor;

        /// <inheritdoc/>
        public CountVisitor(SelectVisitor visitor) : base(visitor, ConditionType.Where)
        {
            _visitor = visitor;
        }

        /// <inheritdoc/>
        protected override void StartupCore(MethodCallExpression node)
        {
            Writer.Keyword(SqlKeyword.SELECT);

            Writer.Write("COUNT");

            Writer.OpenBrace();

            using (var domain = Writer.Domain())
            {
                Visit(node.Arguments[0]);

                domain.Flyback();

                if (_buildSelect)
                {
                    Writer.Write('*');

                    Writer.CloseBrace();
                }
            }
        }

        /// <inheritdoc/>
        protected override void LinqCore(MethodCallExpression node)
        {
            var name = node.Method.Name;

            switch (name)
            {
                case nameof(Queryable.Select):

                    _buildSelect = false;

                    if (_isDistinct)
                    {
                        Writer.Keyword(SqlKeyword.DISTINCT);
                    }

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        domain.Flyback();

                        Select(node.Arguments[1]);

                        Writer.CloseBrace();
                    }

                    break;
                case nameof(Queryable.Distinct):

                    _isDistinct = true;

                    Visit(node.Arguments[0]);

                    break;
                default:

                    _visitor.Visit(node);

                    break;
            }
        }

        /// <inheritdoc/>
        protected override void Select(Expression node)
        {
            if (_isDistinct)
            {
                using (var visitor = new SelectListVisitor(this))
                {
                    visitor.Startup(node);
                }
            }
            else
            {
                using (var visitor = new CountSelectListVisitor(this))
                {
                    visitor.Startup(node);
                }
            }
        }

        #region 内嵌类
        private class CountSelectListVisitor : SelectListVisitor
        {
            public CountSelectListVisitor(CoreVisitor visitor) : base(visitor, false)
            {

            }

            protected override void Lambda<T>(Expression<T> node)
            {
                if (node.Body.Type.IsCell())
                {
                    base.Lambda(node);
                }
                else
                {
                    Writer.Write("*");
                }
            }
        }
        #endregion
    }
}