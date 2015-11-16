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
            public MachineId Client;
            public int Command;

            public Request(MachineId client, int command)
                : base()
            {
                this.Client = client;
                this.Command = command;
            }
        }

        internal class Response : Event { }
        internal class ResponseError : Event { }

        private class LocalEvent : Event { }

        MachineId Server;
        
        int LatestCommand;
        int Counter;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(NotifyServer), nameof(UpdateServer))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.LatestCommand = -1;
            this.Counter = 0;
        }

        void UpdateServer()
        {
            this.Server = (this.ReceivedEvent as NotifyServer).Server;
            this.Raise(new LocalEvent());
        }

        [OnEntry(nameof(PumpRequestOnEntry))]
        [OnEventDoAction(typeof(NotifyServer), nameof(UpdateServer))]
        [OnEventDoAction(typeof(Response), nameof(ProcessResponse))]
        [OnEventDoAction(typeof(ResponseError), nameof(RetryRequest))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        class PumpRequest : MachineState { }

        void PumpRequestOnEntry()
        {
            this.LatestCommand = new Random().Next(100);
            this.Counter++;

            this.Send(this.Server, new Request(this.Id, this.LatestCommand));
        }

        void ProcessResponse()
        {
            if (this.Counter == 3)
            {
                this.Raise(new Halt());
            }
            else
            {
                this.Raise(new LocalEvent());
            }
        }

        void RetryRequest()
        {
            this.Send(this.Server, new Request(this.Id, this.LatestCommand));
        }
    }
}
