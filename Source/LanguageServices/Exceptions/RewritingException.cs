// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// Exception thrown during rewriting.
    /// </summary>
    public class RewritingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingException"/> class.
        /// </summary>
        public RewritingException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingException"/> class.
        /// </summary>
        public RewritingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
