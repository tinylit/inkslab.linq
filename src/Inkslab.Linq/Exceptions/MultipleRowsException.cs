using Inkslab.Exceptions;

namespace Inkslab.Linq.Exceptions
{
    /// <summary>
    /// 多行数据异常。
    /// </summary>
    public class MultipleRowsException : CodeException
    {
        /// <inheritdoc/>
        public MultipleRowsException(string message, int errorCode = 409) : base(message, errorCode)
        {
        }
    }
}
