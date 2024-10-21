using System;
using System.Data;

namespace Inkslab.Linq.Exceptions
{
    /// <inheritdoc/>
    public class DSyntaxErrorException : SyntaxErrorException
    {
        /// <inheritdoc/>
        public DSyntaxErrorException()
        {
        }

        /// <inheritdoc/>
        public DSyntaxErrorException(string s) : base(s)
        {
        }

        /// <inheritdoc/>
        public DSyntaxErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
