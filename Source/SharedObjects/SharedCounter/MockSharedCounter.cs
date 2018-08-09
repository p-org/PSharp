//-----------------------------------------------------------------------
// <copyright file="MockSharedCounter.cs">
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

using System.Threading.Tasks;
using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared counter modeled using a state-machine for testing.
    /// </summary>
    internal sealed class MockSharedCounter : ISharedCounter
    {
        /// <summary>
        /// The testing runtime hosting this shared counter.
        /// </summary>
        private readonly ITestingRuntime Runtime;

        /// <summary>
        /// Machine modeling the shared counter.
        /// </summary>
        private MachineId CounterMachine;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">The P# runtime instance.</param>
        internal MockSharedCounter(ITestingRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <summary>
        /// Initializes the shared counter.
        /// </summary>
        /// <param name="value">The initial value of the counter.</param>
        /// <returns>Task that represents the asynchronous operation.</returns>
        internal async Task InitializeAsync(int value)
        {
            this.CounterMachine = await this.Runtime.CreateMachineAsync(typeof(SharedCounterMachine));
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.CounterMachine, SharedCounterEvent.SetEvent(currentMachine.Id, value));
            await currentMachine.Receive(typeof(SharedCounterResponseEvent));
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
            return this.Runtime.SendEventAsync(this.CounterMachine, SharedCounterEvent.IncrementEvent());
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
            return this.Runtime.SendEventAsync(this.CounterMachine, SharedCounterEvent.DecrementEvent());
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
        public async Task<int> GetValueAsync()
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.CounterMachine, SharedCounterEvent.GetEvent(currentMachine.Id));
            var response = await currentMachine.Receive(typeof(SharedCounterResponseEvent));
            return (response as SharedCounterResponseEvent).Value;
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
        public async Task<int> AddAsync(int value)
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.CounterMachine, SharedCounterEvent.AddEvent(currentMachine.Id, value));
            var response = await currentMachine.Receive(typeof(SharedCounterResponseEvent));
            return (response as SharedCounterResponseEvent).Value;
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
        public async Task<int> ExchangeAsync(int value)
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.CounterMachine, SharedCounterEvent.SetEvent(currentMachine.Id, value));
            var response = await currentMachine.Receive(typeof(SharedCounterResponseEvent));
            return (response as SharedCounterResponseEvent).Value;
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
        public async Task<int> CompareExchangeAsync(int value, int comparand)
        {
            var currentMachine = this.Runtime.GetCurrentMachine();
            await this.Runtime.SendEventAsync(this.CounterMachine, SharedCounterEvent.CasEvent(currentMachine.Id, value, comparand));
            var response = await currentMachine.Receive(typeof(SharedCounterResponseEvent));
            return (response as SharedCounterResponseEvent).Value;
        }
    }
}
