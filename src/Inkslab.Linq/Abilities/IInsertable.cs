using System;
using System.Linq.Expressions;
using Inkslab.Annotations;

namespace Inkslab.Linq.Abilities
{
    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    [Ignore]
    public interface IInsertable<TEntity> : ICommandExecutor
    {
        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        ICommandExecutor Limit(string[] columns);

        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        ICommandExecutor Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        ICommandExecutor Except(string[] columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        ICommandExecutor Except<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 启用自增主键反写：执行后将数据库生成的 ID 回填到对应实体属性上。
        /// 要求实体满足「单主键 + <c>[DatabaseGenerated]</c>」；
        /// 不满足时调用 <see cref="ICommandExecutor.Execute"/> 抛 <see cref="System.InvalidOperationException"/>。
        /// </summary>
        /// <remarks>
        /// 反写的都是数据库为<b>每一行</b>真实生成的 ID，不做任何「ID 连续」假设。<br/>
        /// 引擎支持矩阵（能净则净，余者 fail-fast）：<br/>
        /// • 反写（非 Ignore）：PostgreSQL（RETURNING）、SqlServer（OUTPUT INSERTED）、MySQL（LAST_INSERT_ID()）、
        ///   SQLite（RETURNING，需 3.35+）、DB2（FINAL TABLE）、Sybase（@@IDENTITY）；Oracle 不支持批量自增反写。<br/>
        /// • Ignore + 反写：仅 PostgreSQL / MySQL / SQLite（具备原生 INSERT 忽略且能配合 id 返回）。<br/>
        /// 各路径行为（按「能大批量绝不小批量，能小批量绝不单行」分级）：<br/>
        /// • RETURNING 族（PG/SqlServer/SQLite/DB2，非 Ignore）：单条多值语句逐行返回，&gt;100 行自动拆批，往返 ⌈N/100⌉。<br/>
        /// • 标量族（MySQL/Sybase，含 MySQL Ignore）：无多行 RETURNING，改为单条命令内多组「单行 INSERT + 标量回读」
        ///   （MySQL Ignore 另读 <c>ROW_COUNT()</c> 判定是否被跳过），按参数预算分块，往返 ⌈N/K⌉——绝不逐行往返。<br/>
        /// • RETURNING 族 Ignore（PG/SQLite）：逐行执行，按 RETURNING 行数判定是否反写，被冲突跳过的实体保持原值。<br/>
        /// 不满足「单主键 + <c>[DatabaseGenerated]</c>」时抛 <see cref="System.InvalidOperationException"/>；
        /// 引擎或 Ignore 组合不支持时抛 <see cref="System.NotSupportedException"/>（均为 fail-fast，于本方法调用处抛出）。
        /// </remarks>
        IInsertable<TEntity> PopulateIdentity();
    }
}
