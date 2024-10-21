using Inkslab.Exceptions;

namespace Inkslab.Linq.Exceptions
{
    /// <summary>
    /// 无元素异常。
    /// </summary>
    public class NoElementException : CodeException
    {
        /// <inheritdoc/>
        public NoElementException(string message, int errorCode = 1) : base(message, errorCode)
        {
        }
    }
}
