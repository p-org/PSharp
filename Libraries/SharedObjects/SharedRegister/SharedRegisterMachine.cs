//-----------------------------------------------------------------------
// <copyright file="SharedRegisterMachine.cs">
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

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// A shared register modeled using a state-machine for testing.
    /// </summary>
    internal sealed class SharedRegisterMachine<T> : Machine where T: struct
    {
        /// <summary>
        /// The value of the shared register.
        /// </summary>
        T Value;

        /// <summary>
        /// The start state of this machine.
        /// </summary>
        [Start]
        [OnEntry(nameof(Initialize))]
        [OnEventDoAction(typeof(SharedRegisterEvent), nameof(ProcessEvent))]
        class Init : MachineState { }

        /// <summary>
        /// Initializes the machine.
        /// </summary>
        void Initialize()
        {
            Value = default(T);
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedRegisterEvent;
            switch (e.Operation)
            {
                case SharedRegisterEvent.SharedRegisterOperation.SET:
                    Value = (T)e.Value;
                    break;

                case SharedRegisterEvent.SharedRegisterOperation.GET:
                    this.Send(e.Sender, new SharedRegisterResponseEvent<T>(Value));
                    break;

                case SharedRegisterEvent.SharedRegisterOperation.UPDATE:
                    var func = (Func<T, T>)e.Func;
                    Value = func(Value);
                    this.Send(e.Sender, new SharedRegisterResponseEvent<T>(Value));
                    break;
            }
        }
    }
}
