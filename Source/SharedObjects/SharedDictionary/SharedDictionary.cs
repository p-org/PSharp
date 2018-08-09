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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices;

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
        /// <param name="runtime">The P# runtime instance.</param>
        /// <returns>The result is the <see cref="ISharedDictionary{TKey, TValue}"/>.</returns>
        [Obsolete("Please use SharedDictionary.CreateAsync(...) instead.")]
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(IPSharpRuntime runtime)
        {
            return CreateAsync<TKey, TValue>(null, runtime).Result;
        }

        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <param name="runtime">The P# runtime instance.</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is
        /// the <see cref="ISharedDictionary{TKey, TValue}"/>.
        /// </returns>
        public static Task<ISharedDictionary<TKey, TValue>> CreateAsync<TKey, TValue>(IPSharpRuntime runtime)
        {
            return CreateAsync<TKey, TValue>(null, runtime);
        }

        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <param name="comparer">Comparer for keys.</param>
        /// <param name="runtime">The P# runtime instance.</param>
        /// <returns>The result is the <see cref="ISharedDictionary{TKey, TValue}"/>.</returns>
        [Obsolete("Please use SharedDictionary.CreateAsync(...) instead.")]
        public static ISharedDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey> comparer, IPSharpRuntime runtime)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>(comparer);
            }
            else if (runtime is ITestingRuntime testingRuntime)
            {
                var dictionary = new MockSharedDictionary<TKey, TValue>(testingRuntime);
                dictionary.InitializeAsync(comparer).Wait();
                return dictionary;
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }

        /// <summary>
        /// Creates a new shared dictionary.
        /// </summary>
        /// <param name="comparer">Comparer for keys.</param>
        /// <param name="runtime">IPSharpRuntime</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is
        /// the <see cref="ISharedDictionary{TKey, TValue}"/>.
        /// </returns>
        public static async Task<ISharedDictionary<TKey, TValue>> CreateAsync<TKey, TValue>(IEqualityComparer<TKey> comparer, IPSharpRuntime runtime)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedDictionary<TKey, TValue>(comparer);
            }
            else if (runtime is ITestingRuntime testingRuntime)
            {
                var dictionary = new MockSharedDictionary<TKey, TValue>(testingRuntime);
                await dictionary.InitializeAsync(comparer);
                return dictionary;
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
