//-----------------------------------------------------------------------
// <copyright file="SharedCounterMachine.cs">
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

namespace Microsoft.PSharp.SharedObjects
{
    /// <summary>
    /// A shared counter modeled using a state-machine for testing.
    /// </summary>
    internal sealed class SharedCounterMachine : Machine
    {
        /// <summary>
        /// The value of the shared counter.
        /// </summary>
        int Counter;

        /// <summary>
        /// The start state of this machine.
        /// </summary>
        [Start]
        [OnEntry(nameof(Initialize))]
        [OnEventDoAction(typeof(SharedCounterEvent), nameof(ProcessEvent))]
        class Init : MachineState { }

        /// <summary>
        /// Initializes the machine.
        /// </summary>
        void Initialize()
        {
            Counter = 0;
        }

        /// <summary>
        /// Processes the next dequeued event.
        /// </summary>
        void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedCounterEvent;
            switch (e.Operation)
            {
                case SharedCounterEvent.SharedCounterOperation.SET:
                    Counter = e.Value;
                    break;
                case SharedCounterEvent.SharedCounterOperation.GET:
                    Send(e.Sender, new SharedCounterResponseEvent(Counter));
                    break;
                case SharedCounterEvent.SharedCounterOperation.INC:
                    Counter++;
                    break;
                case SharedCounterEvent.SharedCounterOperation.DEC:
                    Counter--;
                    break;
            }
        }
    }
}
