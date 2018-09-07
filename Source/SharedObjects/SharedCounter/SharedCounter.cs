// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Shared counter that can be safely shared by multiple P# machines.
    /// </summary>
    public static class SharedCounter
    {
        /// <summary>
        /// Creates a new shared counter.
        /// </summary>
        /// <param name="runtime">IPSharpRuntime</param>
        /// <param name="value">Initial value</param>
        public static ISharedCounter Create(IPSharpRuntime runtime, int value = 0)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedCounter(value);
            }
            else if (runtime is ITestingRuntime testingRuntime)
            {
                return new MockSharedCounter(value, testingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
