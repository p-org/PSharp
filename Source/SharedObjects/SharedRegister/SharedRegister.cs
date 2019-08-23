﻿using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Shared register that can be safely shared by multiple P# machines.
    /// </summary>
    public static class SharedRegister
    {
        /// <summary>
        /// Creates a new shared register.
        /// </summary>
        /// <param name="runtime">The machine runtime.</param>
        /// <param name="value">The initial value.</param>
        public static ISharedRegister<T> Create<T>(IMachineRuntime runtime, T value = default)
            where T : struct
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedRegister<T>(value);
            }
            else if (runtime is SystematicTestingRuntime testingRuntime)
            {
                return new MockSharedRegister<T>(value, testingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
