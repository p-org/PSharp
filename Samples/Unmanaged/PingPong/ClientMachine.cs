using System;
using Microsoft.PSharp;

using PingPongWrapper;

namespace PingPong
{
    internal class ClientMachine : Machine
    {
        ClientWrapper Client;
        MachineId Server;
        int Counter;

        [Start]
        [OnEventDoAction(typeof(Events.ConfigEvent), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

        void Configure()
        {
            this.Server = (this.ReceivedEvent as Events.ConfigEvent).id;
            this.Counter = 0;

            this.Client = new ClientWrapper(this.Server);

            this.Raise(new Unit());
        }
        
        [OnEventDoAction(typeof(Events.MessageEvent), nameof(SendPing))]
        class Active : MachineState { }

        private void SendPing()
        {
            Console.WriteLine("\nTurns: {0} / 5\n", this.Counter);
            if (this.Counter == 5)
            {
                this.Raise(new Halt());
            }

            this.Counter++;
            this.Client.invoke(new Unit());
        }
    }
}
