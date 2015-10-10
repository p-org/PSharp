using System;
using Microsoft.PSharp;

using PingPongWrapper;

namespace PingPong
{
    internal class ClientMachine : Machine
    {
        ClientWrapper Client;
        Id Server;
        int Counter;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Server = (Id)this.Payload;
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
