//-----------------------------------------------------------------------
// <copyright file="ProductionSharedRegister.cs">
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
    /// Implements a shared register to be used in production.
    /// </summary>
    internal sealed class ProductionSharedRegister<T> : ISharedRegister<T> where T : struct
    {
        /// <summary>
        /// The current value of the register.
        /// </summary>
        private T Value;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public ProductionSharedRegister(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        /// <returns>The result is the current value.</returns>
        public T GetValue()
        {
            return this.GetValueAsync().Result;
        }

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation. The task result is the current value.</returns>
        public Task<T> GetValueAsync()
        {
            T currentValue;
            lock (this)
            {
                currentValue = Value;
            }

            return Task.FromResult(currentValue);
        }

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetValue(T value)
        {
            this.SetValueAsync(value).Wait();
        }

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        public Task SetValueAsync(T value)
        {
            lock (this)
            {
                this.Value = value;
            }
#if NET45
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        /// <param name="func">The function to use for updating the value.</param>
        /// <returns>The result is the new value of the register.</returns>
        public T Update(Func<T, T> func)
        {
            return this.UpdateAsync(func).Result;
        }

        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        /// <param name="func">The function to use for updating the value.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the new value of the register.</returns>
        public Task<T> UpdateAsync(Func<T, T> func)
        {
            T oldValue, newValue;
            bool done = false;

            do
            {
                oldValue = Value;
                newValue = func(oldValue);

                lock (this)
                {
                    if (oldValue.Equals(Value))
                    {
                        Value = newValue;
                        done = true;
                    }
                }
            } while (!done);

            return Task.FromResult(newValue);
        }
    }
}
