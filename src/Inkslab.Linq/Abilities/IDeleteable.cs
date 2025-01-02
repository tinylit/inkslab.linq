using Inkslab.Annotations;

namespace Inkslab.Linq.Abilities
{
    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    [Ignore]
    public interface IDeleteable<TEntity> : IDeleteableTimeout<TEntity>
    {
        /// <summary>
        /// 设置超时时间。
        /// </summary>
        /// <param name="commandTimeout">命令执行超时时间。</param>
        /// <returns>超时删除能力。</returns>
        IDeleteableTimeout<TEntity> Timeout(int commandTimeout);
    }

    /// <summary>
    /// 超时。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IDeleteableTimeout<TEntity> : ICommandExecutor
    {
        /// <summary>
        /// 跳过幂等验证。
        /// </summary>
        /// <returns></returns>
        ICommandExecutor SkipIdempotentValid();
    }
}
