using Inkslab.Annotations;

namespace Inkslab.Linq.Abilities
{
    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    [Ignore]
    public interface IDeleteable<TEntity> : ICommandExecutor
    {
        /// <summary>
        /// 跳过幂等验证。
        /// </summary>
        /// <returns></returns>
        ICommandExecutor SkipIdempotentValid();
    }
}
