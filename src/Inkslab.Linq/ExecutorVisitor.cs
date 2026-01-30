using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;
using Inkslab.Linq.Expressions;

namespace Inkslab.Linq
{
    /// <summary>
    /// 执行器适配器。
    /// </summary>
    public class ExecutorVisitor : BaseVisitor, IExecutorVisitor
    {
        private int? _commandTimeout;

        /// <inheritdoc/>
        public ExecutorVisitor(DbStrictAdapter adapter)
            : base(adapter) { }

        /// <inheritdoc/>
        protected override void Startup(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case nameof(QueryableExtentions.Delete) when Engine == DatabaseEngine.SQLite: //? SQLite 不支持别名删除。
                    goto default;
                case nameof(QueryableExtentions.Delete) when Engine is DatabaseEngine.SqlServer or DatabaseEngine.Sybase:
                    using (var visitor = new SQLServerDeleteVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(QueryableExtentions.Delete):
                    using (var visitor = new DeleteVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(QueryableExtentions.Update) when Engine is DatabaseEngine.MySQL or DatabaseEngine.Sybase:
                    using (var visitor = new MySQLUpdateVisitor(this, node.Arguments[^1]))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(QueryableExtentions.Update) when Engine == DatabaseEngine.SqlServer:
                    using (var visitor = new SQLServerUpdateVisitor(this, node.Arguments[^1]))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(QueryableExtentions.Update) when Engine == DatabaseEngine.PostgreSQL:
                    using (var visitor = new PostgreSQLUpdateVisitor(this, node.Arguments[^1]))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(QueryableExtentions.Update) when Engine == DatabaseEngine.Oracle:
                    using (var visitor = new OracleUpdateVisitor(this, node.Arguments[^1]))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(QueryableExtentions.Insert):
                    using (var visitor = new InsertVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                default:
                    throw new NotSupportedException($"当前数据库引擎{Engine}的方法“{node.Method.Name}”不被支持！");
            }
        }

        /// <inheritdoc/>
        public sealed override void Startup(Expression node)
        {
            if (node.NodeType == ExpressionType.Call)
            {
                base.Startup(node);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        protected override void MethodCall(MethodCallExpression node) => Backflow(this, node);

        /// <summary>
        /// 链路回流。
        /// </summary>
        /// <param name="visitor">当前访问器。</param>
        /// <param name="node">节点。</param>
        protected virtual void Backflow(ExpressionVisitor visitor, MethodCallExpression node)
        {
            string name = node.Method.Name;

            switch (name)
            {
                case nameof(QueryableExtentions.Timeout):

                    int timeOut = node.Arguments[1].GetValueFromExpression<int>();

                    if (_commandTimeout.HasValue)
                    {
                        _commandTimeout += timeOut;
                    }
                    else
                    {
                        _commandTimeout = timeOut;
                    }

                    visitor.Visit(node.Arguments[0]);

                    break;

                default:
                    throw new DSyntaxErrorException($"方法“{name}”不被支持！");
            }
        }

        /// <summary>
        /// 转SQL。
        /// </summary>
        /// <returns>SQL命令。</returns>
        public virtual CommandSql ToSQL()
        {
            string sql = Writer.ToString();

            return new CommandSql(sql, Writer.Parameters, _commandTimeout);
        }

        #region 内嵌类。
        private class DeleteAllVisitor : BaseVisitor
        {
            private readonly ExecutorVisitor _visitor;

            /// <inheritdoc/>
            public DeleteAllVisitor(ExecutorVisitor visitor)
                : base(visitor)
            {
                _visitor = visitor;
            }

            /// <inheritdoc/>
            protected override void Startup(MethodCallExpression node)
            {
                switch (node.Method.Name)
                {
                    case nameof(QueryableExtentions.Delete):

                        Visit(node.Method.IsStatic ? node.Arguments[0] : node.Object); //? 全表删除。

                        break;

                    default:

                        base.Startup(node);

                        break;
                }
            }

            /// <inheritdoc/>
            protected override void Constant(IQueryable value)
            {
                Writer.Keyword(SqlKeyword.DELETE);
                Writer.Keyword(SqlKeyword.FROM);

                Name();
            }

            protected override void MethodCall(MethodCallExpression node) =>
                _visitor.Backflow(this, node);
        }

        private class SQLServerDeleteVisitor : ScriptVisitor
        {
            private readonly ExecutorVisitor _visitor;

            /// <inheritdoc/>
            public SQLServerDeleteVisitor(ExecutorVisitor visitor)
                : base(visitor, ConditionType.Where)
            {
                _visitor = visitor;
            }

            /// <inheritdoc/>
            protected override void LinqCore(MethodCallExpression node)
            {
                switch (node.Method.Name)
                {
                    case nameof(QueryableExtentions.Delete):

                        var instanceArg = node.Method.IsStatic ? node.Arguments[0] : node.Object;

                        if (node.Arguments.Count > (node.Method.IsStatic ? 1 : 0))
                        {
                            Where(node);
                        }
                        else
                        {
                            Visit(instanceArg);
                        }

                        break;

                    default:

                        base.LinqCore(node);

                        break;
                }
            }

            protected override void DataSourceMode()
            {
                Writer.Keyword(SqlKeyword.DELETE);
            }

            protected override void Constant(IQueryable value)
            {
                DataSourceMode();

                Nickname();

                Writer.Keyword(SqlKeyword.FROM);

                Name();

                TableAs();
            }

            protected override void Backflow(
                ExpressionVisitor visitor,
                MethodCallExpression node
            ) => _visitor.Backflow(visitor, node);
        }

        private class DeleteVisitor : ScriptVisitor
        {
            private readonly ExecutorVisitor _visitor;

            /// <inheritdoc/>
            public DeleteVisitor(ExecutorVisitor visitor)
                : base(visitor, ConditionType.Where)
            {
                _visitor = visitor;
            }

            /// <inheritdoc/>
            protected override void LinqCore(MethodCallExpression node)
            {
                switch (node.Method.Name)
                {
                    case nameof(QueryableExtentions.Delete):

                        var instanceArg = node.Method.IsStatic ? node.Arguments[0] : node.Object;

                        if (node.Arguments.Count > (node.Method.IsStatic ? 1 : 0))
                        {
                            Where(node);
                        }
                        else
                        {
                            Visit(instanceArg);
                        }

                        break;

                    default:

                        base.LinqCore(node);

                        break;
                }
            }

            protected override void DataSourceMode()
            {
                Writer.Keyword(SqlKeyword.DELETE);
                Writer.Keyword(SqlKeyword.FROM);
            }

            protected override void Backflow(
                ExpressionVisitor visitor,
                MethodCallExpression node
            ) => _visitor.Backflow(visitor, node);
        }

        private class MySQLUpdateVisitor : ScriptVisitor
        {
            private readonly ExecutorVisitor _visitor;
            private readonly Expression _bodySetter;

            /// <inheritdoc/>
            public MySQLUpdateVisitor(ExecutorVisitor visitor, Expression bodySetter)
                : base(visitor, ConditionType.Where)
            {
                _visitor = visitor;
                _bodySetter = bodySetter;
            }

            /// <inheritdoc/>
            protected override void LinqCore(MethodCallExpression node)
            {
                switch (node.Method.Name)
                {
                    case nameof(QueryableExtentions.Update):

                        Visit(node.Method.IsStatic ? node.Arguments[0] : node.Object);

                        break;

                    default:

                        base.LinqCore(node);

                        break;
                }
            }

            protected override void DataSourceMode()
            {
                Writer.Keyword(SqlKeyword.UPDATE);
            }

            protected override void Constant(IQueryable value)
            {
                var tableInfo = Table();

                base.Constant(value);

                Writer.Keyword(SqlKeyword.SET);

                var updateFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (var visitor = new SetVisitor(this, tableInfo, updateFields))
                {
                    visitor.Visit(_bodySetter);
                }

                if (updateFields.Count == 0)
                {
                    throw new DSyntaxErrorException("请指定更新字段！");
                }

                if (tableInfo.Versions.Count > 0)
                {
                    foreach (var (name, version) in tableInfo.Versions)
                    {
                        if (version == VersionKind.None || updateFields.Contains(name))
                        {
                            continue;
                        }

                        if (tableInfo.ReadOnlys.Contains(name))
                        {
                            continue;
                        }

                        if (!tableInfo.Fields.TryGetValue(name, out var field))
                        {
                            continue;
                        }

                        Writer.Delimiter();

                        ParameterSchema();

                        Writer.Name(field);

                        Writer.Write(" = ");

                        switch (version)
                        {
                            case VersionKind.Increment:

                                ParameterSchema();

                                Writer.Name(field);

                                Writer.Operator(SqlOperator.Add);

                                Writer.Constant(1);

                                break;
                            case VersionKind.Ticks:
                                Writer.Constant(DateTime.Now.Ticks);

                                break;
                            case VersionKind.Now:
                                switch (Engine)
                                {
                                    case DatabaseEngine.MySQL:
                                    case DatabaseEngine.PostgreSQL:
                                        Writer.Write("NOW()");
                                        break;
                                    case DatabaseEngine.SqlServer:
                                    case DatabaseEngine.Sybase:
                                        Writer.Write("GETDATE()");
                                        break;
                                    default:
                                        Writer.Constant(DateTime.Now);
                                        break;
                                }

                                break;
                            case VersionKind.Timestamp:
                                Writer.Constant(DateTime.UtcNow - DateTime.UnixEpoch);

                                break;
                            default:
                                throw new NotSupportedException(
                                    $"不支持“{tableInfo.Name}”表字段“{field}”版本“{version}”处理！"
                                );
                        }
                    }
                }
            }

            protected override void Backflow(
                ExpressionVisitor visitor,
                MethodCallExpression node
            ) => _visitor.Backflow(visitor, node);

            #region 内嵌类。
            private class SetVisitor : BaseVisitor
            {
                private readonly MySQLUpdateVisitor _visitor;
                private readonly ITableInfo _tableInfo;
                private readonly HashSet<string> _updateFields;

                public SetVisitor(
                    MySQLUpdateVisitor visitor,
                    ITableInfo tableInfo,
                    HashSet<string> updateFields
                )
                    : base(visitor)
                {
                    _visitor = visitor;
                    _tableInfo = tableInfo;
                    _updateFields = updateFields;
                }

                protected override void Member(MemberInfo memberInfo, Expression node)
                {
                    if (!_updateFields.Add(memberInfo.Name))
                    {
                        throw new DSyntaxErrorException($"字段“{memberInfo.Name}”重复指定!");
                    }

                    if (_tableInfo.ReadOnlys.Contains(memberInfo.Name))
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”是只读字段!");
                    }

                    if (_tableInfo.Fields.TryGetValue(memberInfo.Name, out string field))
                    {
                        _visitor.ParameterSchema();

                        Writer.Write(field);

                        Writer.Write(" = ");

                        base.Member(memberInfo, node);
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”不是有效的数据库字段!");
                    }
                }

                protected override void Lambda<T>(Expression<T> node)
                {
                    _visitor.PreparingParameter(node);

                    base.Lambda(node);
                }
            }
            #endregion
        }

        private class SQLServerUpdateVisitor : ScriptVisitor
        {
            private readonly ExecutorVisitor _visitor;
            private readonly Expression _bodySetter;

            /// <inheritdoc/>
            public SQLServerUpdateVisitor(ExecutorVisitor visitor, Expression bodySetter)
                : base(visitor, ConditionType.Where)
            {
                _visitor = visitor;
                _bodySetter = bodySetter;
            }

            /// <inheritdoc/>
            protected override void LinqCore(MethodCallExpression node)
            {
                switch (node.Method.Name)
                {
                    case nameof(QueryableExtentions.Update):

                        Visit(node.Method.IsStatic ? node.Arguments[0] : node.Object);

                        break;

                    default:

                        base.LinqCore(node);

                        break;
                }
            }

            protected override void Constant(IQueryable value)
            {
                var tableInfo = Table();

                Writer.Keyword(SqlKeyword.UPDATE);

                Nickname();

                Writer.Keyword(SqlKeyword.SET);

                var updateFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (var visitor = new SetVisitor(this, tableInfo, updateFields))
                {
                    visitor.Visit(_bodySetter);
                }

                if (updateFields.Count == 0)
                {
                    throw new DSyntaxErrorException("请指定更新字段！");
                }

                if (tableInfo.Versions.Count > 0)
                {
                    foreach (var (name, version) in tableInfo.Versions)
                    {
                        if (version == VersionKind.None || updateFields.Contains(name))
                        {
                            continue;
                        }

                        if (tableInfo.ReadOnlys.Contains(name))
                        {
                            continue;
                        }

                        if (!tableInfo.Fields.TryGetValue(name, out var field))
                        {
                            continue;
                        }

                        Writer.Delimiter();

                        ParameterSchema();

                        Writer.Name(field);

                        Writer.Write(" = ");

                        switch (version)
                        {
                            case VersionKind.Increment:

                                ParameterSchema();

                                Writer.Name(field);

                                Writer.Operator(SqlOperator.Add);

                                Writer.Constant(1);

                                break;
                            case VersionKind.Ticks:
                                Writer.Constant(DateTime.Now.Ticks);

                                break;
                            case VersionKind.Now:
                                switch (Engine)
                                {
                                    case DatabaseEngine.MySQL:
                                    case DatabaseEngine.PostgreSQL:
                                        Writer.Write("NOW()");
                                        break;
                                    case DatabaseEngine.SqlServer:
                                    case DatabaseEngine.Sybase:
                                        Writer.Write("GETDATE()");
                                        break;
                                    default:
                                        Writer.Constant(DateTime.Now);
                                        break;
                                }

                                break;
                            case VersionKind.Timestamp:
                                Writer.Constant(DateTime.UtcNow - DateTime.UnixEpoch);

                                break;
                            default:
                                throw new NotSupportedException(
                                    $"不支持“{tableInfo.Name}”表字段“{field}”版本“{version}”处理！"
                                );
                        }
                    }
                }

                base.Constant(value);
            }

            protected override void Backflow(
                ExpressionVisitor visitor,
                MethodCallExpression node
            ) => _visitor.Backflow(visitor, node);

            #region 内嵌类。
            private class SetVisitor : BaseVisitor
            {
                private readonly SQLServerUpdateVisitor _visitor;
                private readonly ITableInfo _tableInfo;
                private readonly HashSet<string> _updateFields;

                public SetVisitor(
                    SQLServerUpdateVisitor visitor,
                    ITableInfo tableInfo,
                    HashSet<string> updateFields
                )
                    : base(visitor)
                {
                    _visitor = visitor;
                    _tableInfo = tableInfo;
                    _updateFields = updateFields;
                }

                protected override void Member(MemberInfo memberInfo, Expression node)
                {
                    if (!_updateFields.Add(memberInfo.Name))
                    {
                        throw new DSyntaxErrorException($"字段“{memberInfo.Name}”重复指定!");
                    }

                    if (_tableInfo.ReadOnlys.Contains(memberInfo.Name))
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”是只读字段!");
                    }

                    if (_tableInfo.Fields.TryGetValue(memberInfo.Name, out string field))
                    {
                        Writer.Write(field);

                        Writer.Write(" = ");

                        base.Member(memberInfo, node);
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”不是有效的数据库字段!");
                    }
                }

                protected override void Lambda<T>(Expression<T> node)
                {
                    _visitor.PreparingParameter(node);

                    base.Lambda(node);
                }
            }
            #endregion
        }

        private class PostgreSQLUpdateVisitor : ScriptVisitor
        {
            private readonly ExecutorVisitor _visitor;
            private readonly Expression _bodySetter;

            /// <inheritdoc/>
            public PostgreSQLUpdateVisitor(ExecutorVisitor visitor, Expression bodySetter)
                : base(visitor, ConditionType.Where)
            {
                _visitor = visitor;
                _bodySetter = bodySetter;
            }

            /// <inheritdoc/>
            protected override void LinqCore(MethodCallExpression node)
            {
                switch (node.Method.Name)
                {
                    case nameof(QueryableExtentions.Update):

                        Visit(node.Method.IsStatic ? node.Arguments[0] : node.Object);

                        break;

                    default:

                        base.LinqCore(node);

                        break;
                }
            }

            protected override void DataSourceMode()
            {
                Writer.Keyword(SqlKeyword.UPDATE);
            }

            protected override void Constant(IQueryable value)
            {
                var tableInfo = Table();

                base.Constant(value);

                Writer.Keyword(SqlKeyword.SET);

                var updateFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (var visitor = new SetVisitor(this, tableInfo, updateFields))
                {
                    visitor.Visit(_bodySetter);
                }

                if (updateFields.Count == 0)
                {
                    throw new DSyntaxErrorException("请指定更新字段！");
                }

                if (tableInfo.Versions.Count > 0)
                {
                    foreach (var (name, version) in tableInfo.Versions)
                    {
                        if (version == VersionKind.None || updateFields.Contains(name))
                        {
                            continue;
                        }

                        if (tableInfo.ReadOnlys.Contains(name))
                        {
                            continue;
                        }

                        if (!tableInfo.Fields.TryGetValue(name, out var field))
                        {
                            continue;
                        }

                        Writer.Delimiter();

                        Writer.Name(field);

                        Writer.Write(" = ");

                        switch (version)
                        {
                            case VersionKind.Increment:

                                Writer.Name(field);

                                Writer.Operator(SqlOperator.Add);

                                Writer.Constant(1);

                                break;
                            case VersionKind.Ticks:
                                Writer.Constant(DateTime.Now.Ticks);

                                break;
                            case VersionKind.Now:
                                switch (Engine)
                                {
                                    case DatabaseEngine.MySQL:
                                    case DatabaseEngine.PostgreSQL:
                                        Writer.Write("NOW()");
                                        break;
                                    case DatabaseEngine.SqlServer:
                                    case DatabaseEngine.Sybase:
                                        Writer.Write("GETDATE()");
                                        break;
                                    default:
                                        Writer.Constant(DateTime.Now);
                                        break;
                                }

                                break;
                            case VersionKind.Timestamp:
                                Writer.Constant(DateTime.UtcNow - DateTime.UnixEpoch);

                                break;
                            default:
                                throw new NotSupportedException(
                                    $"不支持“{tableInfo.Name}”表字段“{field}”版本“{version}”处理！"
                                );
                        }
                    }
                }
            }

            protected override void Backflow(
                ExpressionVisitor visitor,
                MethodCallExpression node
            ) => _visitor.Backflow(visitor, node);

            #region 内嵌类。
            private class SetVisitor : BaseVisitor
            {
                private readonly PostgreSQLUpdateVisitor _visitor;
                private readonly ITableInfo _tableInfo;
                private readonly HashSet<string> _updateFields;

                public SetVisitor(
                    PostgreSQLUpdateVisitor visitor,
                    ITableInfo tableInfo,
                    HashSet<string> updateFields
                )
                    : base(visitor)
                {
                    _visitor = visitor;
                    _tableInfo = tableInfo;
                    _updateFields = updateFields;
                }

                protected override void Member(MemberInfo memberInfo, Expression node)
                {
                    if (!_updateFields.Add(memberInfo.Name))
                    {
                        throw new DSyntaxErrorException($"字段“{memberInfo.Name}”重复指定!");
                    }

                    if (_tableInfo.ReadOnlys.Contains(memberInfo.Name))
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”是只读字段!");
                    }

                    if (_tableInfo.Fields.TryGetValue(memberInfo.Name, out string field))
                    {
                        Writer.Write(field);

                        Writer.Write(" = ");

                        base.Member(memberInfo, node);

                        if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
                        {
                            var lookupDbType = LookupDb.For(node.Type);

                            if (lookupDbType.IsJsonbType())
                            {
                                Writer.Write("::jsonb");
                            }
                            else if (lookupDbType.IsJsonType())
                            {
                                Writer.Write("::json");
                            }
                        }
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”不是有效的数据库字段!");
                    }
                }

                protected override void Lambda<T>(Expression<T> node)
                {
                    _visitor.PreparingParameter(node);

                    base.Lambda(node);
                }
            }
            #endregion
        }

        private class OracleUpdateVisitor : ScriptVisitor
        {
            private readonly ExecutorVisitor _visitor;
            private readonly Expression _bodySetter;

            /// <inheritdoc/>
            public OracleUpdateVisitor(ExecutorVisitor visitor, Expression bodySetter)
                : base(visitor, ConditionType.Where)
            {
                _visitor = visitor;
                _bodySetter = bodySetter;
            }

            /// <inheritdoc/>
            protected override void LinqCore(MethodCallExpression node)
            {
                switch (node.Method.Name)
                {
                    case nameof(QueryableExtentions.Update):

                        Writer.Keyword(SqlKeyword.UPDATE);

                        Writer.OpenBrace();

                        Visit(node.Method.IsStatic ? node.Arguments[0] : node.Object);

                        Writer.CloseBrace();

                        var tableInfo = Table();

                        Writer.Keyword(SqlKeyword.SET);

                        var updateFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        using (var visitor = new SetVisitor(this, tableInfo, updateFields))
                        {
                            visitor.Visit(_bodySetter);
                        }

                        if (updateFields.Count == 0)
                        {
                            throw new DSyntaxErrorException("请指定更新字段！");
                        }

                        if (tableInfo.Versions.Count > 0)
                        {
                            foreach (var (name, version) in tableInfo.Versions)
                            {
                                if (version == VersionKind.None || updateFields.Contains(name))
                                {
                                    continue;
                                }

                                if (tableInfo.ReadOnlys.Contains(name))
                                {
                                    continue;
                                }

                                if (!tableInfo.Fields.TryGetValue(name, out var field))
                                {
                                    continue;
                                }

                                Writer.Delimiter();

                                Writer.Name(field);

                                Writer.Write(" = ");

                                switch (version)
                                {
                                    case VersionKind.Increment:

                                        Writer.Name(field);

                                        Writer.Operator(SqlOperator.Add);

                                        Writer.Constant(1);

                                        break;
                                    case VersionKind.Ticks:
                                        Writer.Constant(DateTime.Now.Ticks);

                                        break;
                                    case VersionKind.Now:
                                        switch (Engine)
                                        {
                                            case DatabaseEngine.MySQL:
                                            case DatabaseEngine.PostgreSQL:
                                                Writer.Write("NOW()");
                                                break;
                                            case DatabaseEngine.SqlServer:
                                            case DatabaseEngine.Sybase:
                                                Writer.Write("GETDATE()");
                                                break;
                                            default:
                                                Writer.Constant(DateTime.Now);
                                                break;
                                        }

                                        break;
                                    case VersionKind.Timestamp:
                                        Writer.Constant(DateTime.UtcNow - DateTime.UnixEpoch);

                                        break;
                                    default:
                                        throw new NotSupportedException(
                                            $"不支持“{tableInfo.Name}”表字段“{field}”版本“{version}”处理！"
                                        );
                                }
                            }
                        }

                        break;

                    default:

                        base.LinqCore(node);

                        break;
                }
            }

            protected override void DataSourceMode()
            {
                Writer.Keyword(SqlKeyword.UPDATE);
            }

            protected override void Constant(IQueryable value)
            {
                Writer.Keyword(SqlKeyword.SELECT);

                Select(value.Expression);

                DataSourceMode();

                Name();

                TableAs();
            }

            protected override void Backflow(
                ExpressionVisitor visitor,
                MethodCallExpression node
            ) => _visitor.Backflow(visitor, node);

            #region 内嵌类。
            private class SetVisitor : BaseVisitor
            {
                private readonly OracleUpdateVisitor _visitor;
                private readonly ITableInfo _tableInfo;
                private readonly HashSet<string> _updateFields;

                public SetVisitor(
                    OracleUpdateVisitor visitor,
                    ITableInfo tableInfo,
                    HashSet<string> updateFields
                )
                    : base(visitor)
                {
                    _visitor = visitor;
                    _tableInfo = tableInfo;
                    _updateFields = updateFields;
                }

                protected override void Member(MemberInfo memberInfo, Expression node)
                {
                    if (!_updateFields.Add(memberInfo.Name))
                    {
                        throw new DSyntaxErrorException($"字段“{memberInfo.Name}”重复指定!");
                    }

                    if (_tableInfo.ReadOnlys.Contains(memberInfo.Name))
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”是只读字段!");
                    }

                    if (_tableInfo.Fields.TryGetValue(memberInfo.Name, out string field))
                    {
                        Writer.Write(field);

                        Writer.Write(" = ");

                        base.Member(memberInfo, node);
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”不是有效的数据库字段!");
                    }
                }

                protected override void Lambda<T>(Expression<T> node)
                {
                    _visitor.PreparingParameter(node);

                    base.Lambda(node);
                }
            }
            #endregion
        }

        private class InsertVisitor : ScriptVisitor
        {
            private bool _ignoreConflict;

            private readonly ExecutorVisitor _visitor;

            /// <inheritdoc/>
            public InsertVisitor(ExecutorVisitor visitor)
                : base(visitor, ConditionType.Where)
            {
                _visitor = visitor;
            }

            /// <inheritdoc/>
            protected override void DataSourceMode()
            {
                Writer.Keyword(SqlKeyword.INTO);
            }

            /// <inheritdoc/>
            protected override void TableAs()
            {
                //? 插入语句，不写入别名。
            }

            /// <inheritdoc/>
            protected override void LinqCore(MethodCallExpression node)
            {
                switch (node.Method.Name)
                {
                    case nameof(QueryableExtentions.Insert):

                        Writer.Keyword(SqlKeyword.INSERT);

                        base.Visit(node.Arguments[0]);

                        Writer.OpenBrace();

                        var tableInfo = Table();

                        var insertFields = new HashSet<string>(tableInfo.Fields.Count);

                        using (var domain = Writer.Domain())
                        {
                            using (
                                var visitor = new InsertSelectVisitor(this, tableInfo, insertFields)
                            )
                            {
                                visitor.Startup(node.Arguments[1]);

                                if (insertFields.Count == 0)
                                {
                                    throw new DSyntaxErrorException("未指定插入字段！");
                                }
                            }

                            domain.Flyback();

                            bool commaFlag = false;

                            foreach (var name in insertFields)
                            {
                                if (commaFlag)
                                {
                                    Writer.Delimiter();
                                }
                                else
                                {
                                    commaFlag = true;
                                }

                                Writer.Name(name);
                            }

                            Writer.CloseBrace();

                            Writer.WhiteSpace();
                        }

                        if (_ignoreConflict)
                        {
                            Writer.Write(" ON CONFLICT DO NOTHING");
                        }

                        break;
                    case nameof(QueryableExtentions.Ignore):

                        if (Engine == DatabaseEngine.SQLite)
                        {
                            Writer.Keyword(SqlKeyword.OR);
                        }

                        if (Engine == DatabaseEngine.PostgreSQL)
                        {
                            _ignoreConflict = true;
                        }
                        else
                        {
                            Writer.Keyword(SqlKeyword.IGNORE);
                        }

                        base.Visit(node.Arguments[0]);

                        break;
                    default:
                        base.LinqCore(node);

                        break;
                }
            }

            protected override void Backflow(
                ExpressionVisitor visitor,
                MethodCallExpression node
            ) => _visitor.Backflow(visitor, node);

            #region 内嵌类。
            private class InsertSelectVisitor : SelectVisitor
            {
                private readonly ITableInfo _tableInfo;
                private readonly HashSet<string> _insertFields;

                public InsertSelectVisitor(
                    InsertVisitor visitor,
                    ITableInfo tableInfo,
                    HashSet<string> insertFields
                )
                    : base(visitor)
                {
                    _tableInfo = tableInfo;
                    _insertFields = insertFields;
                }

                protected override void Select(Expression node)
                {
                    using (
                        var visitor = new InsertSelectListVisitor(this, _tableInfo, _insertFields)
                    )
                    {
                        visitor.Startup(node);
                    }
                }
            }

            private class InsertSelectListVisitor : SelectListVisitor
            {
                private readonly ITableInfo _tableInfo;
                private readonly HashSet<string> _insertFields;

                public InsertSelectListVisitor(
                    CoreVisitor visitor,
                    ITableInfo tableInfo,
                    HashSet<string> insertFields
                )
                    : base(visitor)
                {
                    _tableInfo = tableInfo;
                    _insertFields = insertFields;
                }

                protected override void Member(MemberInfo memberInfo, Expression node)
                {
                    if (_tableInfo.ReadOnlys.Contains(memberInfo.Name))
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”是只读字段!");
                    }

                    if (_tableInfo.Fields.TryGetValue(memberInfo.Name, out string value))
                    {
                        if (!_insertFields.Add(value))
                        {
                            throw new DSyntaxErrorException($"字段“{memberInfo.Name}”重复指定!");
                        }

                        base.Member(memberInfo, node);

                        if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
                        {
                            var lookupDbType = LookupDb.For(node.Type);

                            if (lookupDbType.IsJsonbType())
                            {
                                Writer.Write("::jsonb");
                            }
                            else if (lookupDbType.IsJsonType())
                            {
                                Writer.Write("::json");
                            }
                        }
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"“{memberInfo.Name}”不是有效的数据库字段!");
                    }
                }

                protected override void Member(string schema, string field, string name)
                {
                    if (_tableInfo.ReadOnlys.Contains(name))
                    {
                        throw new DSyntaxErrorException($"“{name}”是只读字段!");
                    }

                    if (_tableInfo.Fields.TryGetValue(name, out string value))
                    {
                        if (!_insertFields.Add(value))
                        {
                            throw new DSyntaxErrorException($"字段“{name}”重复指定!");
                        }

                        base.Member(schema, field, name);
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"“{name}”不是有效的数据库字段!");
                    }
                }
            }
            #endregion
        }

        #endregion
    }
}
