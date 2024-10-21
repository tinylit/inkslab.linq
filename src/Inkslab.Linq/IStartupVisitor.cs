using System;
using System.Linq.Expressions;

namespace Inkslab.Linq
{
    /// <summary>
    /// 启动访问器。
    /// </summary>
    public interface IStartupVisitor : IDisposable
    {
        /// <summary>
        /// 启动。
        /// </summary>
        /// <param name="node">分析表达式。</param>
        /// <returns></returns>
        void Startup(Expression node);
    }
}
