// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
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
        /// Constructor.
        /// </summary>
        /// <param name="message">Message</param>
        public RewritingException(string message)
            : base(message)
        {

        }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Exception</param>
        public RewritingException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
