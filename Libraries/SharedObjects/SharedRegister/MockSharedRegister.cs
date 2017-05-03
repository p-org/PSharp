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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Implements a shared register
    /// </summary>
    internal sealed class MockSharedRegister<T> : ISharedRegister<T> where T: struct
    {
        /// <summary>
        /// The register
        /// </summary>
        MachineId registerMachine;

        BugFindingRuntime Runtime;

        /// <summary>
        /// Initializes the register
        /// </summary>
        /// <param name="value">Initial value</param>
        /// <param name="Runtime">Runtime</param>
        public MockSharedRegister(T value, BugFindingRuntime Runtime)
        {
            this.Runtime = Runtime;
            registerMachine = Runtime.CreateMachine(typeof(SharedRegisterMachine<T>));
            Runtime.SendEvent(registerMachine, SharedRegisterEvent.SetEvent(value));
        }


        /// <summary>
        /// Read and update the register
        /// </summary>
        /// <param name="func">Update function</param>
        /// <returns>Resulting value of the register</returns>
        public T Update(Func<T, T> func)
        {
            var currentMachine = Runtime.GetCurrentMachine();
            Runtime.SendEvent(registerMachine, SharedRegisterEvent.UpdateEvent(func, currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
            return e.value;
        }

        /// <summary>
        /// Gets current value of the register
        /// </summary>
        /// <returns>Current value</returns>
        public T GetValue()
        {
            var currentMachine = Runtime.GetCurrentMachine();
            Runtime.SendEvent(registerMachine, SharedRegisterEvent.GetEvent(currentMachine.Id));
            var e = currentMachine.Receive(typeof(SharedRegisterResponseEvent<T>)).Result as SharedRegisterResponseEvent<T>;
            return e.value;
        }

        /// <summary>
        /// Sets current value of the register
        /// </summary>
        /// <param name="value">Value</param>
        public void SetValue(T value)
        {
            Runtime.SendEvent(registerMachine, SharedRegisterEvent.SetEvent(value));
        }
    }
}
