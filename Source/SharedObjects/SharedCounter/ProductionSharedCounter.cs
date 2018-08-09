//-----------------------------------------------------------------------
// <copyright file="ProductionSharedCounter.cs">
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

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Implements a shared counter to be used in production.
    /// </summary>
    internal sealed class ProductionSharedCounter : ISharedCounter
    {
        /// <summary>
        /// The value of the shared counter.
        /// </summary>
        private volatile int Counter;

        /// <summary>
        /// Initializes the shared counter.
        /// </summary>
        /// <param name="value">The initial value of the counter.</param>
        public ProductionSharedCounter(int value)
        {
            this.Counter = value;
        }

        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        public void Increment()
        {
            this.IncrementAsync().Wait();
        }

        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public Task IncrementAsync()
        {
            Interlocked.Increment(ref this.Counter);
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        public void Decrement()
        {
            this.DecrementAsync().Wait();
        }

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public Task DecrementAsync()
        {
            Interlocked.Decrement(ref this.Counter);
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        /// <returns>The result is the current value.</returns>
        public int GetValue()
        {
            return this.GetValueAsync().Result;
        }

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation. The task result is the current value.</returns>
        public Task<int> GetValueAsync()
        {
            return Task.FromResult(this.Counter);
        }

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        /// <param name="value">Value to add.</param>
        /// <returns>The result is the new value.</returns>
        public int Add(int value)
        {
            return this.AddAsync(value).Result;
        }

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        /// <param name="value">Value to add.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the new value.</returns>
        public Task<int> AddAsync(int value)
        {
            return Task.FromResult(Interlocked.Add(ref this.Counter, value));
        }

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <returns>The result is the original value.</returns>
        public int Exchange(int value)
        {
            return this.ExchangeAsync(value).Result;
        }

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the original value.</returns>
        public Task<int> ExchangeAsync(int value)
        {
            return Task.FromResult(Interlocked.Exchange(ref this.Counter, value));
        }

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="comparand">Value to compare against.</param>
        /// <returns>The result is the original value.</returns>
        public int CompareExchange(int value, int comparand)
        {
            return this.CompareExchangeAsync(value, comparand).Result;
        }

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="comparand">Value to compare against.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the original value.</returns>
        public Task<int> CompareExchangeAsync(int value, int comparand)
        {
            return Task.FromResult(Interlocked.CompareExchange(ref this.Counter, value, comparand));
        }
    }
}
