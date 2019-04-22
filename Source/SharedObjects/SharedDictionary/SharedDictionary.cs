// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Shared dictionary that can be safely shared by multiple P# machines.
    /// </summary>
    public static class SharedDictionary
    {
        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <param name="runtime">The machine runtime.</param>
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(IMachineRuntime runtime)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>();
            }
            else if (runtime is TestingRuntime testingRuntime)
            {
                return new MockSharedDictionary<TKey, TValue>(null, testingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }

        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <param name="comparer">The key comparer.</param>
        /// <param name="runtime">The machine runtime.</param>
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey> comparer, IMachineRuntime runtime)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>(comparer);
            }
            else if (runtime is TestingRuntime testingRuntime)
            {
                return new MockSharedDictionary<TKey, TValue>(comparer, testingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
