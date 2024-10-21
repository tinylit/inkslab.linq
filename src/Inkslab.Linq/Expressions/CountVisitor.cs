using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 计数。
    /// </summary>
    public class CountVisitor : ScriptVisitor
    {
        /// <summary>
        /// 生成 SELECT。
        /// </summary>
        private bool buildSelect = true;

        /// <summary>
        /// 去重。
        /// </summary>
        private bool isDistinct = false;


        private readonly BaseSelectVisitor _visitor;

        /// <inheritdoc/>
        public CountVisitor(BaseSelectVisitor visitor) : base(visitor, ConditionType.Where)
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

                if (buildSelect)
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

                    buildSelect = false;

                    if (isDistinct)
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

                    isDistinct = true;

                    Visit(node.Arguments[0]);

                    break;
                default:

                    _visitor.Visit(node);

                    break;
            }
        }

        /// <inheritdoc/>
        protected virtual void Select(Expression node)
        {
            using (var visitor = new SelectListVisitor(this))
            {
                visitor.Startup(node);
            }
        }
    }
}