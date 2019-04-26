// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// An operation used during scheduling.
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// starts executing.
        /// </summary>
        Start = 0,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// creates another <see cref="ISchedulable"/>.
        /// </summary>
        Create,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// sends an event to a target <see cref="ISchedulable"/>.
        /// </summary>
        Send,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// receives an event.
        /// </summary>
        Receive,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/>
        /// stops executing.
        /// </summary>
        Stop,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/> yields. This denotes
        /// that the current <see cref="ISchedulable"/> is not making progress. An
        /// unfair scheduler could disable these <see cref="ISchedulable"/> until
        /// quiescence, and then re-enable them.
        /// 
        /// This operation is not currently supported in P#.
        /// </summary>
        Yield,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/> wants to wait for
        /// quiescence. A scheduler could disable the <see cref="ISchedulable"/>
        /// until quiescence, and then re-enable it.
        /// 
        /// This operation is not currently supported in P#.
        /// </summary>
        WaitForQuiescence,
        /// <summary>
        /// Operation used when an <see cref="ISchedulable"/> wants to wait for
        /// another <see cref="ISchedulable"/> to <see cref="Stop"/>.
        /// 
        /// This operation is not currently supported in P#.
        /// </summary>
        Join,
        /// <summary>
        /// KG HAX
        /// </summary>
        HAX_Dummy
    }
}
