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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.TestingServices
{
    internal sealed class SharedCounterMachine : Machine
    {
        int counter = 0;

        [Start]
        [OnEventDoAction(typeof(SharedCounterEvent), nameof(ProcessEvent))]
        class Init : MachineState { }

        void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedCounterEvent;
            switch (e.op)
            {
                case SharedCounterEvent.SharedCounterOp.SET:
                    counter = e.value;
                    break;
                case SharedCounterEvent.SharedCounterOp.GET:
                    this.Send(e.sender, new SharedCounterResponseEvent(counter));
                    break;
                case SharedCounterEvent.SharedCounterOp.INC:
                    counter++;
                    break;
                case SharedCounterEvent.SharedCounterOp.DEC:
                    counter--;
                    break;
            }

        }
    }
}
