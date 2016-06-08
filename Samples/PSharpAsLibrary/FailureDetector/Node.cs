using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace FailureDetector
{
    internal class Node : Machine
    {
        internal class Ping : Event
        {
            public MachineId Sender;

            public Ping(MachineId sender)
                : base()
            {
                this.Sender = sender;
            }
        }

        internal class Pong : Event
        {
            public MachineId Node;

            public Pong(MachineId node)
                : base()
            {
                this.Node = node;
            }
        }

        [Start]
        [OnEventDoAction(typeof(Ping), nameof(SendPong))]
        class WaitPing : MachineState { }

        void SendPong()
        {
            var sender = (this.ReceivedEvent as Ping).Sender;
            this.Monitor<Safety>(new Safety.MPong(this.Id));
            this.Send(sender, new Pong(this.Id));
        }
    }
}
