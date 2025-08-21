using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库执行器。
    /// </summary>
    public interface IDatabaseExecutor
    {
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>查询结果。</returns>
        T Read<T>(IConnection databaseStrings, CommandSql<T> commandSql);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>查询结果列表。</returns>
        List<T> Query<T>(IConnection databaseStrings, CommandSql commandSql);

        /// <summary>
        /// 查询多个结果。
        /// </summary>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>查询器。</returns>
        IDbGridReader QueryMultiple(IConnection databaseStrings, CommandSql commandSql);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>受影响的行数。</returns>
        int Execute(IConnection databaseStrings, CommandSql commandSql);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="dataTable">数据表。</param>
        /// <param name="commandTimeout">超时时间（秒）。</param>
        int WriteToServer(IConnection databaseStrings, DataTable dataTable, int? commandTimeout = null);

        /// <summary>
        /// 多执行异步处理器。
        /// </summary>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="multipleAction">执行方法。</param>
        /// <param name="commandTimeout">超时时间（秒）。</param>
        /// <returns>受影响的行数。</returns>
        int ExecuteMultiple(IConnection databaseStrings, Action<IMultipleExecutor> multipleAction, int? commandTimeout = null);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <param name="cancellationToken">取消操作的标记。</param>
        /// <returns>查询结果。</returns>
        Task<T> ReadAsync<T>(IConnection databaseStrings, CommandSql<T> commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>异步查询结果迭代器。</returns>
        IAsyncEnumerable<T> QueryAsync<T>(IConnection databaseStrings, CommandSql commandSql);

        /// <summary>
        /// 查询多个结果。
        /// </summary>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>异步查询器。</returns>
        Task<IAsyncDbGridReader> QueryMultipleAsync(IConnection databaseStrings, CommandSql commandSql);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <param name="cancellationToken">取消操作的标记。</param>
        /// <returns>受影响的行数。</returns>
        Task<int> ExecuteAsync(IConnection databaseStrings, CommandSql commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="dataTable">数据表。</param>
        /// <param name="commandTimeout">超时时间（秒）。</param>
        /// <param name="cancellationToken">取消操作的标记。</param>
        Task<int> WriteToServerAsync(IConnection databaseStrings, DataTable dataTable, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 多执行异步处理器。
        /// </summary>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="multipleAction">执行方法。</param>
        /// <param name="commandTimeout">超时时间（秒）。</param>
        /// <param name="cancellationToken">取消操作的标记。</param>
        /// <returns>受影响的行数。</returns>
        Task<int> ExecuteMultipleAsync(IConnection databaseStrings, Func<IAsyncMultipleExecutor, Task> multipleAction, int? commandTimeout = null, CancellationToken cancellationToken = default);
    }
}
