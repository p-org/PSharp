// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The exception that is thrown in a P# machine upon cancellation
    /// of execution by the P# runtime.
    /// </summary>
    public sealed class ExecutionCanceledException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        internal ExecutionCanceledException() { }
    }
}
