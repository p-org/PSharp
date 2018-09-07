// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Shared register that can be safely shared by multiple P# state-machines.
    /// </summary>
    public static class SharedRegister
    {
        /// <summary>
        /// Creates a new shared register.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        /// <param name="value">Initial value</param>
        public static ISharedRegister<T> Create<T>(PSharpRuntime runtime, T value = default(T)) where T : struct
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedRegister<T>(value);
            }
            else if (runtime is TestingServices.BugFindingRuntime)
            {
                return new MockSharedRegister<T>(value, runtime as BugFindingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
