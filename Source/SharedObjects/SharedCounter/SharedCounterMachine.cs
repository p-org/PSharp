// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
                    Send(e.Sender, new SharedCounterResponseEvent(Counter));
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
                case SharedCounterEvent.SharedCounterOperation.ADD:
                    Counter += e.Value;
                    Send(e.Sender, new SharedCounterResponseEvent(Counter));
                    break;
                case SharedCounterEvent.SharedCounterOperation.CAS:
                    Send(e.Sender, new SharedCounterResponseEvent(Counter));
                    if (Counter == e.Comparand)
                    {
                        Counter = e.Value;
                    }                    
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException("Unsupported SharedCounter operation: " + e.Operation);
            }
        }
    }
}
