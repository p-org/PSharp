// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// Allows the caller to assert a condition that should be true.
    /// </summary>
    public class ContractAsserter : IContract
    {
        /// <summary>
        /// Assert a condition that should be true.
        /// </summary>
        /// <param name="condition">The condition.</param>
        public void Assert(bool condition)
        {
            Debug.Assert(condition, string.Empty);
        }

        /// <summary>
        /// Assert a condition that should be true.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <param name="msg">An error message if the condition is false.</param>
        public void Assert(bool condition, string msg)
        {
            Debug.Assert(condition, msg);
        }
    }
}
