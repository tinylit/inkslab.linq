using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Options;
using Inkslab.Linq.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Npgsql;
using Xunit;

namespace Inkslab.Linq.Tests
{
    public class DatabaseExecutorLoggingTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PostgreSQLBatchInsert_LogsSqlAndParameterValuesOnlyWhenDebugIsEnabledAsync(bool debugEnabled)
        {
            var logger = new RecordingLogger<PostgreSQLBulkAssistant>(debugEnabled);
            using var connection = new NpgsqlConnection();
            using var bulk = new PostgreSQLBulkAssistant.PostgreSQLBulk(connection, null, logger);
            using var table = new DataTable("log\"table");
            table.Columns.Add("id", typeof(int));
            table.Columns.Add("name", typeof(string));

            // 空表不会执行 SQL，也不应写入日志。
            Assert.Equal(0, bulk.WriteToServer(table));
            Assert.Equal(0, await bulk.WriteToServerAsync(table));
            Assert.Empty(logger.Messages);

            table.Rows.Add(7, "O'Brien");
            Assert.Throws<InvalidOperationException>(() => bulk.WriteToServer(table));
            await Assert.ThrowsAsync<InvalidOperationException>(() => bulk.WriteToServerAsync(table));

            Assert.Equal(debugEnabled ? 2 : 0, logger.Messages.Count);
            Assert.All(logger.Messages, message => Assert.Equal("INSERT INTO \"log\"\"table\" (\"id\", \"name\") VALUES (7, 'O''Brien')", message));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PostgreSQLCopy_LogsGeneratedSqlOnlyWhenDebugIsEnabledAsync(bool debugEnabled)
        {
            var logger = new RecordingLogger<PostgreSQLBulkAssistant>(debugEnabled);
            var services = new ServiceCollection();
            services.AddSingleton<ILogger<PostgreSQLBulkAssistant>>(logger);
            services.AddSingleton<PostgreSQLBulkCopyFactory>();
            using var provider = services.BuildServiceProvider();
            using var connection = new NpgsqlConnection();
            using var bulk = provider.GetRequiredService<PostgreSQLBulkCopyFactory>().Create(connection);
            using var table = new DataTable("log\"table");
            table.Columns.Add("id", typeof(int));
            table.Rows.Add(7);

            // 关闭连接使执行在本地失败；日志仍应先记录待执行的真实 COPY 命令。
            Assert.Throws<InvalidOperationException>(() => bulk.WriteToServer(table));
            await Assert.ThrowsAsync<InvalidOperationException>(() => bulk.WriteToServerAsync(table));

            Assert.Equal(debugEnabled ? 2 : 0, logger.Messages.Count);
            Assert.All(logger.Messages, message => Assert.Equal("COPY \"log\"\"table\" (\"id\") FROM STDIN (FORMAT BINARY)", message));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Commands_LogOncePerExecutionOnlyWhenDebugIsEnabledAsync(bool debugEnabled)
        {
            var logger = new RecordingLogger<DatabaseExecutor>(debugEnabled);
            var pipeline = new TestPipeline();
            var executor = new DatabaseExecutor(pipeline, new DatabaseExecutorOptions(), logger);
            var connection = new TestConnectionStrings();
            var sql = new CommandSql("SELECT @id", new Dictionary<string, object> { ["id"] = 7 });
            var readSql = new CommandSql<int>(sql, RowStyle.First);

            executor.Execute(connection, sql);
            executor.Query<int>(connection, sql);
            executor.Read(connection, readSql);
            using (executor.QueryMultiple(connection, sql)) { }
            executor.ExecuteMultiple(connection, batch => batch.Execute(sql), null);
            executor.ExecuteMultiple(connection, batch => batch.Execute(sql), 30);
            await executor.ExecuteAsync(connection, sql);
            await executor.ReadAsync(connection, readSql);
            await using (await executor.QueryMultipleAsync(connection, sql)) { }
            await executor.ExecuteMultipleAsync(connection, async batch => { await batch.ExecuteAsync(sql); }, null);
            await executor.ExecuteMultipleAsync(connection, async batch => { await batch.ExecuteAsync(sql); }, 30);

            Assert.Equal(11, pipeline.Executions);
            Assert.Equal(debugEnabled ? 11 : 0, logger.Messages.Count);
            Assert.All(logger.Messages, message => Assert.Equal("SELECT 7", message));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AsyncQuery_LogsPerEnumerationAndSkipsFormattingWhenDisabledAsync(bool debugEnabled)
        {
            var logger = new RecordingLogger<DatabaseExecutor>(debugEnabled);
            var pipeline = new TestPipeline();
            var executor = new DatabaseExecutor(pipeline, new DatabaseExecutorOptions(), logger);
            var command = new CountingCommandSql();
            var query = executor.QueryAsync<int>(new TestConnectionStrings(), command);

            Assert.Empty(logger.Messages);
            Assert.Equal(0, command.FormatCalls);

            for (int i = 0; i < 2; i++)
            {
                await foreach (var value in query)
                {
                    Assert.Equal(7, value);
                }
            }

            Assert.Equal(2, pipeline.Executions);
            Assert.Equal(debugEnabled ? 2 : 0, logger.Messages.Count);
            Assert.Equal(debugEnabled ? 2 : 0, command.FormatCalls);
        }

        private sealed class CountingCommandSql : CommandSql
        {
            public CountingCommandSql() : base("SELECT 7") { }
            public int FormatCalls { get; private set; }
            public override string ToString()
            {
                FormatCalls++;
                return base.ToString();
            }
        }

        private sealed class RecordingLogger<T> : ILogger<T>
        {
            private readonly bool _debugEnabled;
            public RecordingLogger(bool debugEnabled) => _debugEnabled = debugEnabled;
            public List<string> Messages { get; } = new List<string>();
            public bool IsEnabled(LogLevel logLevel) => _debugEnabled && logLevel == LogLevel.Debug;
            public IDisposable BeginScope<TState>(TState state) => null;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                => Messages.Add(formatter(state, exception));
        }

        private sealed class TestConnectionStrings : IConnection
        {
            public DatabaseEngine Engine => DatabaseEngine.MySQL;
            public string Strings => "unused";
        }

        private sealed class TestPipeline : IDbConnectionPipeline
        {
            public int Executions { get; set; }
            public DbConnection Get(IConnection databaseStrings) => new TestConnection(this);
            public IDatabaseBulkCopy Create(DbConnection connection, DatabaseEngine engine) => throw new NotSupportedException();
            public Task<IDatabaseBulkCopy> CreateAsync(DbConnection connection, DatabaseEngine engine, CancellationToken cancellationToken)
                => throw new NotSupportedException();
        }

        private sealed class TestConnection : DbConnection
        {
            private readonly TestPipeline _pipeline;
            private ConnectionState _state;
            public TestConnection(TestPipeline pipeline) => _pipeline = pipeline;
            public override string ConnectionString { get; set; }
            public override string Database => "test";
            public override string DataSource => "test";
            public override string ServerVersion => "1";
            public override ConnectionState State => _state;
            public override void Open() => _state = ConnectionState.Open;
            public override void Close() => _state = ConnectionState.Closed;
            public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
            protected override DbCommand CreateDbCommand() => new TestCommand(_pipeline);
        }

        private sealed class TestCommand : DbCommand
        {
            private readonly TestPipeline _pipeline;
            private readonly MySqlCommand _parameters = new MySqlCommand();
            public TestCommand(TestPipeline pipeline) => _pipeline = pipeline;
            public override string CommandText { get; set; }
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection DbConnection { get; set; }
            protected override DbTransaction DbTransaction { get; set; }
            protected override DbParameterCollection DbParameterCollection => _parameters.Parameters;
            protected override DbParameter CreateDbParameter() => new MySqlParameter();
            public override void Cancel() { }
            public override void Prepare() { }
            public override int ExecuteNonQuery() { _pipeline.Executions++; return 1; }
            public override object ExecuteScalar() => throw new NotSupportedException();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                _pipeline.Executions++;
                var table = new DataTable();
                table.Columns.Add("Id", typeof(int));
                table.Rows.Add(7);
                return table.CreateDataReader();
            }
            protected override void Dispose(bool disposing)
            {
                if (disposing) _parameters.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
