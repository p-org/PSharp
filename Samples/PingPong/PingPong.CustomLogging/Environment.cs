using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Environment : Machine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            var server = this.CreateMachine(typeof(Server));
            var client = this.CreateMachine(typeof(Client));
            this.Send(client, new Client.Config(server));
        }
    }
}