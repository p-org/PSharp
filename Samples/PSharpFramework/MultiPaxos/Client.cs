using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class Client : Machine
    {
        List<Id> Servers;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(local), typeof(PumpRequestOne))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.CreateMonitor(typeof(ValidityCheck));
            this.Servers = this.Payload as List<Id>;
            this.Raise(new local());
        }

        [OnEntry(nameof(PumpRequestOneOnEntry))]
        [OnEventGotoState(typeof(response), typeof(PumpRequestTwo))]
        class PumpRequestOne : MachineState { }

        void PumpRequestOneOnEntry()
        {
            this.Monitor<ValidityCheck>(new monitor_client_sent(), 1);

            if (this.Nondet())
            {
                this.Send(this.Servers[0], new update(), 0, 1);
            }
            else
            {
                this.Send(this.Servers[this.Servers.Count - 1], new update(), 0, 1);
            }

            this.Raise(new response());
        }

        [OnEntry(nameof(PumpRequestTwoOnEntry))]
        [OnEventGotoState(typeof(response), typeof(Done))]
        class PumpRequestTwo : MachineState { }

        void PumpRequestTwoOnEntry()
        {
            this.Monitor<ValidityCheck>(new monitor_client_sent(), 2);

            if (this.Nondet())
            {
                this.Send(this.Servers[0], new update(), 0, 2);
            }
            else
            {
                this.Send(this.Servers[this.Servers.Count - 1], new update(), 0, 2);
            }

            this.Raise(new response());
        }

        [OnEntry(nameof(DoneOnEntry))]
        class Done : MachineState { }

        void DoneOnEntry()
        {
            this.Raise(new Halt());
        }
    }
}
