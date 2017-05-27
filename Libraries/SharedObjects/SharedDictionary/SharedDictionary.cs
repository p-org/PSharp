//-----------------------------------------------------------------------
// <copyright file="SharedDictionary.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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
