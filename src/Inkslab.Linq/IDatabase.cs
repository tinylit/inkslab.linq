using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库。
    /// </summary>
    public interface IDatabase : IDatabase<IConnectionStrings>
    {
    }

    /// <summary>
    /// 数据库。
    /// </summary>
    /// <typeparam name="TConnectionStrings">数据库链接配置。</typeparam>
    public interface IDatabase<TConnectionStrings> where TConnectionStrings : IConnectionStrings
    {
        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        T Single<T>(string sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        T SingleOrDefault<T>(string sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        T First<T>(string sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        T FirstOrDefault<T>(string sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 查询数据。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        List<T> Query<T>(string sql, object param, int? commandTimeout = null);

        /// <summary>
        /// 读取多组数据。
        /// </summary>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        IDbGridReader QueryMultiple(string sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        int Execute(string sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">超时时间。</param>
        int WriteToServer(DataTable dt, int? commandTimeout = null);

        /// <summary>
        /// 多执行异步处理器。
        /// </summary>
        /// <param name="multipleAction">执行方法。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns>影响行。</returns>
        int ExecuteMultiple(Action<IMultipleExecutor> multipleAction, int? commandTimeout = null);

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        Task<T> SingleAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        Task<T> SingleOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        Task<T> FirstAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        Task<T> FirstOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询数据。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        Task<List<T>> QueryAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取多组数据。
        /// </summary>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        Task<IAsyncDbGridReader> QueryMultipleAsync(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        Task<int> WriteToServerAsync(DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 多执行异步处理器。
        /// </summary>
        /// <param name="multipleAction">执行方法。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>影响行。</returns>
        Task<int> ExecuteMultipleAsync(Func<IAsyncMultipleExecutor, Task> multipleAction, int? commandTimeout = null, CancellationToken cancellationToken = default);
    }
}