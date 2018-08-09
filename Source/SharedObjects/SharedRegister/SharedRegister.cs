//-----------------------------------------------------------------------
// <copyright file="SharedRegister.cs">
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
    /// Shared register that can be safely shared by multiple P# machines.
    /// </summary>
    public static class SharedRegister
    {
        /// <summary>
        /// Creates a new shared register.
        /// </summary>
        /// <param name="runtime">IPSharpRuntime</param>
        /// <param name="value">Initial value</param>
        /// <returns>The result is the <see cref="ISharedRegister{T}"/>.</returns>
        [Obsolete("Please use SharedRegister.CreateAsync(...) instead.")]
        public static ISharedRegister<T> Create<T>(IPSharpRuntime runtime, T value = default(T)) where T : struct
        {
            return CreateAsync<T>(runtime, value).Result;
        }

        /// <summary>
        /// Creates a new shared register.
        /// </summary>
        /// <param name="runtime">IPSharpRuntime</param>
        /// <param name="value">Initial value</param>
        /// <returns>
        /// Task that represents the asynchronous operation. The task result is the <see cref="ISharedRegister{T}"/>.
        /// </returns>
        public static async Task<ISharedRegister<T>> CreateAsync<T>(IPSharpRuntime runtime, T value = default(T)) where T : struct
        {
            if (runtime is ProductionRuntime)
            {
                return new ProductionSharedRegister<T>(value);
            }
            else if (runtime is ITestingRuntime testingRuntime)
            {
                var register = new MockSharedRegister<T>(testingRuntime);
                await register.InitializeAsync(value);
                return register;
            }
            else
            {
                throw new RuntimeException("Unknown runtime object of type: " + runtime.GetType().Name + ".");
            }
        }
    }
}
