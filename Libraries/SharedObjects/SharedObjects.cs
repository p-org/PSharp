using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Creates objects that can be shared by multiple P# machines
    /// </summary>
    public static class SharedObjects
    {
        /// <summary>
        /// Creates a shared counter
        /// </summary>
        /// <param name="runtime">PSharp runtime</param>
        /// <param name="value">Initial value</param>
        public static ISharedCounter CreateSharedCounter(PSharpRuntime runtime, int value = 0)
        {
            if (runtime is StateMachineRuntime)
            {
                return new ProductionSharedCounter(value);
            }
            else if (runtime is TestingServices.BugFindingRuntime)
            {
                return new TestingServices.MockSharedCounter(value, runtime as TestingServices.BugFindingRuntime);
            }
            else
            {
                throw new PSharp.RuntimeException("Unknown runtime object type: " + runtime.GetType().Name);
            }
        }

        /// <summary>
        /// Creates a shared counter
        /// </summary>
        /// <param name="runtime">PSharp runtime</param>
        /// <param name="value">Initial value</param>
        public static ISharedRegister<T> CreateSharedRegister<T>(PSharpRuntime runtime, T value = default(T)) where T: struct
        {
            if (runtime is StateMachineRuntime)
            {
                return new ProductionSharedRegister<T>(value);
            }
            else if (runtime is TestingServices.BugFindingRuntime)
            {
                return new TestingServices.MockSharedRegister<T>(value, runtime as TestingServices.BugFindingRuntime);
            }
            else
            {
                throw new PSharp.RuntimeException("Unknown runtime object type: " + runtime.GetType().Name);
            }
        }

        /// <summary>
        /// Creates a shared dictionary
        /// </summary>
        /// <param name="runtime">PSharp runtime</param>
        public static ISharedDictionary<TKey, TValue> CreateSharedDictionary<TKey, TValue>(PSharpRuntime runtime) 
        {
            if (runtime is StateMachineRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>();
            }
            else if (runtime is TestingServices.BugFindingRuntime)
            {
                return new TestingServices.MockSharedDictionary<TKey, TValue>(null, runtime as TestingServices.BugFindingRuntime);
            }
            else
            {
                throw new PSharp.RuntimeException("Unknown runtime object type: " + runtime.GetType().Name);
            }
        }

        /// <summary>
        /// Creates a shared dictionary
        /// </summary>
        /// <param name="comparer">Comparer for keys</param>
        /// <param name="runtime">PSharp runtime</param>
        public static ISharedDictionary<TKey, TValue> CreateSharedDictionary<TKey, TValue>(IEqualityComparer<TKey> comparer, PSharpRuntime runtime)
        {
            if (runtime is StateMachineRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>(comparer);
            }
            else if (runtime is TestingServices.BugFindingRuntime)
            {
                return new TestingServices.MockSharedDictionary<TKey, TValue>(comparer, runtime as TestingServices.BugFindingRuntime);
            }
            else
            {
                throw new PSharp.RuntimeException("Unknown runtime object type: " + runtime.GetType().Name);
            }
        }

    }
}
