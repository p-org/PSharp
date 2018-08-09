//-----------------------------------------------------------------------
// <copyright file="SharedCounter.cs">
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
using System.Threading.Tasks;

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
        /// <param name="runtime">The P# runtime instance.</param>
        /// <param name="value">The initial value of the counter.</param>
        /// <returns>The result is the <see cref="ISharedCounter"/>.</returns>
        [Obsolete("Please use SharedCounter.CreateAsync(...) instead.")]
        public static ISharedCounter Create(IPSharpRuntime runtime, int value = 0)
        {
            return CreateAsync(runtime, value).Result;
        }

        /// <summary>
        /// Creates a new shared counter.
        /// </summary>
        /// <param name="runtime">The P# runtime instance.</param>
        /// <param name="value">The initial value of the counter.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the <see cref="ISharedCounter"/>.</returns>
        public static async Task<ISharedCounter> CreateAsync(IPSharpRuntime runtime, int value = 0)
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedCounter(value);
            }
            else if (runtime is ITestingRuntime testingRuntime)
            {
                var counter = new MockSharedCounter(testingRuntime);
                await counter.InitializeAsync(value);
                return counter;
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
