// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Shared dictionary that can be safely shared by multiple P# state-machines.
    /// </summary>
    public static class SharedDictionary
    {
        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(PSharpRuntime runtime)
        {
            if (runtime is StateMachineRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>();
            }
            else if (runtime is TestingServices.BugFindingRuntime)
            {
                return new MockSharedDictionary<TKey, TValue>(null, runtime as BugFindingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }

        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <param name="comparer">Comparer for keys</param>
        /// <param name="runtime">PSharp runtime</param>
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey> comparer, PSharpRuntime runtime)
        {
            if (runtime is StateMachineRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>(comparer);
            }
            else if (runtime is TestingServices.BugFindingRuntime)
            {
                return new MockSharedDictionary<TKey, TValue>(comparer, runtime as BugFindingRuntime);
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
