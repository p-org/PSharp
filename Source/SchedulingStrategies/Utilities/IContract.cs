// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// Interface for a contract that can be used to assert that
    /// a condition in a scheduling strategy should be true.
    /// </summary>
    public interface IContract
    {
        /// <summary>
        /// Assert a condition that should be true.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="msg">An error message if the condition is false.</param>
        void Assert(bool condition, string msg = "");
    }
}