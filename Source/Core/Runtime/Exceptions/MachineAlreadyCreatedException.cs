// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// The exception that is thrown by the P# runtime found another machine with the same ID
    /// </summary>
    public sealed class MachineAlreadyCreatedException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        /// <param name="message">Message</param>
        internal MachineAlreadyCreatedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="innerException">Inner exception</param>
        internal MachineAlreadyCreatedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
