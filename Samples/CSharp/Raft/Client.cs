using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Raft
{
    internal class Client : Machine
    {
        internal class NotifyServer : Event
        {
            public MachineId Server;

            public NotifyServer(MachineId server)
                : base()
            {
                this.Server = server;
            }
        }

        internal class Request : Event
        {
            public int Command;

            public Request(int command)
                : base()
            {
                this.Command = command;
            }
        }

        internal class Response : Event { }

        private class LocalEvent : Event { }

        MachineId Server;
        int Counter;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        [OnEventDoAction(typeof(NotifyServer), nameof(UpdateServer))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Counter = 0;
        }

        void UpdateServer()
        {
            this.Server = (this.ReceivedEvent as NotifyServer).Server;
            this.Raise(new LocalEvent());
        }

        [OnEntry(nameof(PumpRequestOnEntry))]
        [OnEventDoAction(typeof(NotifyServer), nameof(UpdateServer))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        [OnEventGotoState(typeof(Response), typeof(PumpRequest))]
        class PumpRequest : MachineState { }

        void PumpRequestOnEntry()
        {
            var command = new Random().Next(100);
            this.Send(this.Server, new Request(command));

            this.Counter++;
            if (this.Counter == 3)
            {
                this.Raise(new Halt());
            }
        }
    }
}
