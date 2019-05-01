// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.TestingServices.Threading
{
    /// <summary>
    /// Interface of a resource that can be accessed concurrently.
    /// </summary>
    internal interface ISynchronizedResource
    {
        /// <summary>
        /// True if the resource has been acquired, else false.
        /// </summary>
        bool IsAcquired { get; }
    }
}
