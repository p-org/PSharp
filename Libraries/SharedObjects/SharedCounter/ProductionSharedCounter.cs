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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Implements a shared counter
    /// </summary>
    internal sealed class ProductionSharedCounter : ISharedCounter
    {
        /// <summary>
        /// The counter
        /// </summary>
        volatile int counter;
        
        /// <summary>
        /// Initializes the counter
        /// </summary>
        /// <param name="value">Initial value</param>
        public ProductionSharedCounter(int value)
        {
            counter = value;
        }

        /// <summary>
        /// Increments the counter
        /// </summary>
        public void Increment()
        {
            Interlocked.Increment(ref counter);
        }

        /// <summary>
        /// Decrements the counter
        /// </summary>
        public void Decrement()
        {
            Interlocked.Decrement(ref counter);
        }

        /// <summary>
        /// Gets current value of the counter
        /// </summary>
        public int GetValue()
        {
            return counter;
        }
    }
}
