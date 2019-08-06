// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// The exception that is thrown in a P# Machine upon receiving failure error
    /// from it's respection domain.
    /// </summary>
    public sealed class FailureException : RuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FailureException"/> class.
        /// </summary>
        internal FailureException()
        {
        }
    }
}
