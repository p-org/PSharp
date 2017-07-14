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
        volatile int Counter;

        /// <summary>
        /// Initializes the shared counter.
        /// </summary>
        /// <param name="value">Initial value</param>
        public ProductionSharedCounter(int value)
        {
            Counter = value;
        }

        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        public void Increment()
        {
            Interlocked.Increment(ref Counter);
        }

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        public void Decrement()
        {
            Interlocked.Decrement(ref Counter);
        }

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        /// <returns>Current value</returns>
        public int GetValue()
        {
            return Counter;
        }

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        /// <param name="value">Value to add</param>
        /// <returns>The new value of the counter</returns>
        public int Add(int value)
        {
            return Interlocked.Add(ref Counter, value);
        }

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        /// <param name="value">Value to set</param>
        /// <returns>The original value of the counter</returns>
        public int Exchange(int value)
        {
            return Interlocked.Exchange(ref Counter, value);
        }

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        /// <param name="value">Value to set</param>
        /// <param name="comparand">Value to compare against</param>
        /// <returns>The original value of the counter</returns>
        public int CompareExchange(int value, int comparand)
        {
            return Interlocked.CompareExchange(ref Counter, value, comparand);
        }
    }
}
