//-----------------------------------------------------------------------
// <copyright file="MockSharedRegister.cs">
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
using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared register modeled using a state-machine for testing.
    /// </summary>
    internal sealed class MockSharedRegister<T> : ISharedRegister<T> where T: struct
    {
        /// <summary>
        /// The testing runtime hosting this shared register.
        /// </summary>
        private readonly ITestingRuntime Runtime;

        /// <summary>
        /// Machine modeling the shared register.
        /// </summary>
        private MachineId RegisterMachine;

        /// <summary>
        /// Initializes the shared register.
        /// </summary>
        /// <param name="runtime">The P# runtime instance.</param>
        public MockSharedRegister(ITestingRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <summary>
        /// Initializes the shared register.
        /// </summary>
        /// <param name="value">The initial value.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal async Task InitializeAsync(T value)
        {
            this.RegisterMachine = await this.Runtime.CreateMachineAsync(typeof(SharedRegisterMachine<T>));
            await this.Runtime.SendEventAsync(this.RegisterMachine, SharedRegisterEvent.SetEvent(value));
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
        public async Task<T> GetValueAsync()
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.RegisterMachine, SharedRegisterEvent.GetEvent(currentMachine.Id));
            var e = await currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)) as SharedRegisterResponseEvent<T>;
            return e.Value;
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
            return this.Runtime.SendEventAsync(this.RegisterMachine, SharedRegisterEvent.SetEvent(value));
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
        public async Task<T> UpdateAsync(Func<T, T> func)
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.RegisterMachine, SharedRegisterEvent.UpdateEvent(func, currentMachine.Id));
            var e = await currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)) as SharedRegisterResponseEvent<T>;
            return e.Value;
        }
    }
}
