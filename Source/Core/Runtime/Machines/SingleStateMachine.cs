// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

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
        /// Initilizes the state machine on creation.
        /// </summary>
        private async Task _InitOnEntry()
        {
            await InitOnEntry(this.ReceivedEvent);
        }

        /// <summary>
        /// Initilizes the state machine on creation.
        /// </summary>
        /// <param name="e">Initial event provided on machine creation, or null otherwise.</param>
        protected virtual Task InitOnEntry(Event e) => Task.CompletedTask;

        /// <summary>
        /// Process incoming event.
        /// </summary>
        private async Task _ProcessEvent()
        {
            await ProcessEvent(this.ReceivedEvent);
        }

        /// <summary>
        /// Process incoming event.
        /// </summary>
        /// <param name="e">Event.</param>
        protected abstract Task ProcessEvent(Event e);

        /// <summary>
        /// Halts the machine.
        /// </summary>
        private void TerminatingOnEntry()
        {
            this.Raise(new Halt());
        }
    }
}
