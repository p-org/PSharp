// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// An exception that is thrown by the P# runtime.
    /// </summary>
    public class RuntimeException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        internal RuntimeException()
        {

        }

        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        /// <param name="message">Message</param>
        internal RuntimeException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner exception</param>
        internal RuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
