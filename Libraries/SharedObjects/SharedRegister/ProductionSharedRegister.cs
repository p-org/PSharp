//-----------------------------------------------------------------------
// <copyright file="ISharedRegister.cs">
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
    /// Implements a shared register
    /// </summary>
    internal sealed class ProductionSharedRegister<T> : ISharedRegister<T> where T : struct
    {
        /// <summary>
        /// Current value of the register
        /// </summary>
        T value;

        /// <summary>
        /// Initializes the register
        /// </summary>
        /// <param name="value">Initial value</param>
        public ProductionSharedRegister(T value)
        {
            this.value = value;
        }

        /// <summary>
        /// Read and update the register
        /// </summary>
        /// <param name="func">Update function</param>
        /// <returns>Resulting value of the register</returns>
        public T Update(Func<T, T> func)
        {
            T old_value, new_value;
            bool done = false;

            do
            {
                old_value = value;
                new_value = func(old_value);

                lock (this)
                {
                    if (old_value.Equals(value))
                    {
                        value = new_value;
                        done = true;
                    }
                }
            } while (!done);

            return new_value;
        }

        /// <summary>
        /// Gets current value of the register
        /// </summary>
        /// <returns>Current value</returns>
        public T GetValue()
        {
            T current_value;
            lock (this)
            {
                current_value = value;
            }
            return current_value;
        }

        /// <summary>
        /// Sets current value of the register
        /// </summary>
        /// <param name="value">Value</param>
        public void SetValue(T value)
        {
            lock(this)
            {
                this.value = value;
            }
        }

    }
}
