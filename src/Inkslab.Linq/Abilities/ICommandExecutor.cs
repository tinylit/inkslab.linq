using System.Threading;
using System.Threading.Tasks;
using Inkslab.Annotations;

namespace Inkslab.Linq.Abilities
{
    /// <summary>
    /// 命令执行器。
    /// </summary>
    [Ignore]
    public interface ICommandExecutor
    {
        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <returns>影响行。</returns>
        int Execute();

        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
