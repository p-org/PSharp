//-----------------------------------------------------------------------
// <copyright file="ISharedCounter.cs">
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

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Interface of a shared counter.
    /// </summary>
    public interface ISharedCounter
    {
        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        [Obsolete("Please use ISharedCounter.IncrementAsync() instead.")]
        void Increment();

        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        Task IncrementAsync();

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        [Obsolete("Please use ISharedCounter.DecrementAsync() instead.")]
        void Decrement();

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation.</returns>
        Task DecrementAsync();

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        /// <returns>The result is the current value.</returns>
        [Obsolete("Please use ISharedCounter.GetValueAsync() instead.")]
        int GetValue();

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation. The task result is the current value.</returns>
        Task<int> GetValueAsync();

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        /// <param name="value">Value to add.</param>
        /// <returns>The result is the new value.</returns>
        [Obsolete("Please use ISharedCounter.AddAsync(...) instead.")]
        int Add(int value);

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        /// <param name="value">Value to add.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the new value.</returns>
        Task<int> AddAsync(int value);

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <returns>The result is the original value.</returns>
        [Obsolete("Please use ISharedCounter.ExchangeAsync(...) instead.")]
        int Exchange(int value);

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the original value.</returns>
        Task<int> ExchangeAsync(int value);

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="comparand">Value to compare against.</param>
        /// <returns>The result is the original value.</returns>
        [Obsolete("Please use ISharedCounter.CompareExchangeAsync(...) instead.")]
        int CompareExchange(int value, int comparand);

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="comparand">Value to compare against.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the original value.</returns>
        Task<int> CompareExchangeAsync(int value, int comparand);
    }
}
