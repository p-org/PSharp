//-----------------------------------------------------------------------
// <copyright file="Machine.cs">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a single-state machine.
    /// </summary>
    public abstract class SingleStateMachine : Machine
    {
        [Start]
        [OnEntry(nameof(_InitOnEntry))]
        [OnEventGotoState(typeof(Halt), typeof(Terminating))]
        [OnEventDoAction(typeof(WildCardEvent), nameof(_ProcessEvent))]
        sealed class Init : MachineState { }

        [OnEntry(nameof(TerminatingOnEntry))]
        sealed class Terminating : MachineState { }

        /// <summary>
        /// Initilizes the state machine on creation
        /// </summary>
        private async Task _InitOnEntry()
        {
            await InitOnEntry(this.ReceivedEvent);
        }

        /// <summary>
        /// Initilizes the state machine on creation
        /// </summary>
        /// <param name="e">Initial event provided on machine creation, or null otherwise</param>
        protected virtual Task InitOnEntry(Event e)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Process incoming event
        /// </summary>
        private async Task _ProcessEvent()
        {
            await ProcessEvent(this.ReceivedEvent);
        }

        /// <summary>
        /// Process incoming event
        /// </summary>
        /// <param name="e">Event</param>
        protected abstract Task ProcessEvent(Event e);

        /// <summary>
        /// Halts the machine
        /// </summary>
        private void TerminatingOnEntry()
        {
            this.Raise(new Halt());
        }

    }

}
