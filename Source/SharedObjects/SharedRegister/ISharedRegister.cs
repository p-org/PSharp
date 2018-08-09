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
using System.Threading.Tasks;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// Interface of a shared register.
    /// </summary>
    /// <typeparam name="T">Value type of the shared register</typeparam>
    public interface ISharedRegister<T> where T: struct
    {
        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        /// <returns>The result is the current value.</returns>
        [Obsolete("Please use ISharedRegister.GetValue() instead.")]
        T GetValue();

        /// <summary>
        /// Gets current value of the register.
        /// </summary>
        /// <returns>Task that represents the asynchronous operation. The task result is the current value.</returns>
        Task<T> GetValueAsync();

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        /// <param name="value">The value to set.</param>
        [Obsolete("Please use ISharedRegister.SetValue(...) instead.")]
        void SetValue(T value);

        /// <summary>
        /// Sets current value of the register.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        Task SetValueAsync(T value);

        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        /// <param name="func">The function to use for updating the value.</param>
        /// <returns>The result is the new value of the register.</returns>
        [Obsolete("Please use ISharedRegister.Update(...) instead.")]
        T Update(Func<T, T> func);

        /// <summary>
        /// Reads and updates the register.
        /// </summary>
        /// <param name="func">The function to use for updating the value.</param>
        /// <returns>Task that represents the asynchronous operation. The task result is the new value of the register.</returns>
        Task<T> UpdateAsync(Func<T, T> func);
    }
}
