using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 分组。
    /// </summary>
    public class GroupByVisitor : BaseVisitor
    {
        private readonly CoreVisitor _visitor;
        private readonly Dictionary<Type, Expression> _attachmentExpressions;
        private readonly Dictionary<string, Expression> _elementExpressions;
        private readonly Dictionary<MemberInfo, Expression> _keyExpressions;

        /// <inheritdoc/>
        public GroupByVisitor(CoreVisitor visitor, Dictionary<Type, Expression> attachmentExpressions, Dictionary<string, Expression> elementExpressions, Dictionary<MemberInfo, Expression> keyExpressions) : base(visitor)
        {
            _visitor = visitor;
            _attachmentExpressions = attachmentExpressions;
            _elementExpressions = elementExpressions;
            _keyExpressions = keyExpressions;
        }

        /// <inheritdoc/>
        public override void Startup(MethodCallExpression node)
        {
            //? 分析表名称和分组键。
            new KeyVisitor(this, _keyExpressions)
                    .Visit(node.Arguments[1]);

            _visitor.Visit(node.Arguments[0]);

            Writer.Keyword(Enums.SqlKeyword.GROUP);
            Writer.Keyword(Enums.SqlKeyword.BY);

            Visit(node.Arguments[1]);

            if (node.Arguments.Count > 2)
            {
                new ElementVisitor(_attachmentExpressions, _elementExpressions)
                    .Visit(node.Arguments[2]);
            }
            else
            {
                new AttachmentVisitor(_attachmentExpressions)
                    .Visit(node.Arguments[1]);
            }
        }

        #region 内嵌类。

        private abstract class MemberVisitor : ExpressionVisitor
        {
            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                Member(node.Member, node.Expression);

                return node;
            }

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                foreach (var binding in node.Bindings)
                {
                    VisitMemberBinding(binding);
                }

                return node;
            }

            protected abstract void Member(MemberInfo memberInfo, Expression node);

            /// <inheritdoc />
            protected override Expression VisitNew(NewExpression node)
            {
                for (int i = 0; i < node.Members.Count; i++)
                {
                    Member(node.Members[i], node.Arguments[i]);
                }

                return node;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                Member(node.Member, node);

                return node;
            }
        }

        private class KeyVisitor : MemberVisitor
        {
            private readonly GroupByVisitor _visitor;
            private readonly Dictionary<MemberInfo, Expression> _keyExpressions;

            public KeyVisitor(GroupByVisitor visitor, Dictionary<MemberInfo, Expression> keyExpressions)
            {
                _visitor = visitor;
                _keyExpressions = keyExpressions;
            }

            protected override void Member(MemberInfo memberInfo, Expression node)
                => _keyExpressions.Add(memberInfo, node);

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                _visitor.PreparingParameter(node);

                return base.VisitLambda(node);
            }
        }

        private class ElementVisitor : MemberVisitor
        {
            private readonly Dictionary<Type, Expression> _attachmentExpressions;
            private readonly Dictionary<string, Expression> _elementExpressions;

            public ElementVisitor(Dictionary<Type, Expression> attachmentExpressions, Dictionary<string, Expression> elementExpressions)
            {
                _attachmentExpressions = attachmentExpressions;
                _elementExpressions = elementExpressions;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (!node.Type.IsCell())
                {
                    _attachmentExpressions.Add(node.Type, node);

                    return node;
                }

                return base.VisitMember(node);
            }

            protected override void Member(MemberInfo memberInfo, Expression node)
                => _elementExpressions.Add(memberInfo.Name, node);
        }

        private class AttachmentVisitor : ExpressionVisitor
        {
            private readonly Dictionary<Type, Expression> _attachmentExpressions;

            public AttachmentVisitor(Dictionary<Type, Expression> attachmentExpressions)
            {
                _attachmentExpressions = attachmentExpressions;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                var parameterNode = node.Parameters[0];

                _attachmentExpressions.Add(parameterNode.Type, parameterNode);

                return node;
            }
        }

        #endregion
    }
}