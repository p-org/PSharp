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

using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// A wrapper for a shared counter modeled using a state-machine for testing.
    /// </summary>
    internal sealed class MockSharedCounter : ISharedCounter
    {
        /// <summary>
        /// Machine modeling the shared counter.
        /// </summary>
        MachineId CounterMachine;

        /// <summary>
        /// The bug-finding runtime hosting this shared counter.
        /// </summary>
        BugFindingRuntime Runtime;

        /// <summary>
        /// Initializes the shared counter.
        /// </summary>
        /// <param name="value">Initial value</param>
        /// <param name="Runtime">BugFindingRuntime</param>
        public MockSharedCounter(int value, BugFindingRuntime Runtime)
        {
            this.Runtime = Runtime;
            CounterMachine = Runtime.CreateMachine(typeof(SharedCounterMachine));
            Runtime.SendEvent(CounterMachine, SharedCounterEvent.SetEvent(value));
        }

        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        public void Increment()
        {
            Runtime.SendEvent(CounterMachine, SharedCounterEvent.IncrementEvent());
        }

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        public void Decrement()
        {
            Runtime.SendEvent(CounterMachine, SharedCounterEvent.DecrementEvent());
        }

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        /// <returns>Current value</returns>
        public int GetValue()
        {
            var currentMachine = Runtime.GetCurrentMachine();
            Runtime.SendEvent(CounterMachine, SharedCounterEvent.GetEvent(currentMachine.Id));
            var response = currentMachine.Receive(typeof(SharedCounterResponseEvent)).Result;
            return (response as SharedCounterResponseEvent).Value;
        }
    }
}
