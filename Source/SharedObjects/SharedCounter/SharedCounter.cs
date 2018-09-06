// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Shared counter that can be safely shared by multiple P# state-machines.
    /// </summary>
    public static class SharedCounter
    {
        /// <summary>
        /// Creates a new shared counter.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        /// <param name="value">Initial value</param>
        public static ISharedCounter Create(PSharpRuntime runtime, int value = 0)
        {
            if (runtime is StateMachineRuntime)
            {
                return new ProductionSharedCounter(value);
            }
            else if (runtime is TestingServices.BugFindingRuntime)
            {
                return new MockSharedCounter(value, runtime as BugFindingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
