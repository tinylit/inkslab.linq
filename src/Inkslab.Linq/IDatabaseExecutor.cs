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
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>查询结果。</returns>
        T Read<T>(string connectionStrings, CommandSql<T> commandSql);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns></returns>
        List<T> Query<T>(string connectionStrings, CommandSql commandSql);

        /// <summary>
        /// 查询多个结果。
        /// </summary>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="commandSql">-SQL 命令。</param>
        /// <returns>查询器。</returns>
        IDbGridReader QueryMultiple(string connectionStrings, CommandSql commandSql);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>执行影响行。</returns>
        int Execute(string connectionStrings, CommandSql commandSql);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">超时时间。</param>
        int WriteToServer(string connectionStrings, DataTable dt, int? commandTimeout = null);

        /// <summary>
        /// 多执行异步处理器。
        /// </summary>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="multipleAction">执行方法。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns>影响行。</returns>
        int ExecuteMultiple(string connectionStrings, Action<IMultipleExecutor> multipleAction, int? commandTimeout = null);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>结果。</returns>
        Task<T> ReadAsync<T>(string connectionStrings, CommandSql<T> commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>异步迭代器。</returns>
        IAsyncEnumerable<T> QueryAsync<T>(string connectionStrings, CommandSql commandSql);

        /// <summary>
        /// 查询多个结果。
        /// </summary>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="commandSql">-SQL 命令。</param>
        /// <returns>查询器。</returns>
        Task<IAsyncDbGridReader> QueryMultipleAsync(string connectionStrings, CommandSql commandSql);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        Task<int> ExecuteAsync(string connectionStrings, CommandSql commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        Task<int> WriteToServerAsync(string connectionStrings, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 多执行异步处理器。
        /// </summary>
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <param name="multipleAction">执行方法。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>影响行。</returns>
        Task<int> ExecuteMultipleAsync(string connectionStrings, Func<IAsyncMultipleExecutor, Task> multipleAction, int? commandTimeout = null, CancellationToken cancellationToken = default);
    }
}
