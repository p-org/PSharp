using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Server : Machine
    {
        internal class Pong : Event { }

        [Start]
        [OnEventDoAction(typeof(Client.Ping), nameof(SendPong))]
        class Active : MachineState { }

        void SendPong()
        {
            var client = (this.ReceivedEvent as Client.Ping).Client;
            this.Send(client, new Pong());
        }
    }
}