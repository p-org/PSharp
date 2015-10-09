using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class ClientMachine : Machine
    {
        private Id Server;
        private int Counter;

        [Start]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState
        {
            protected override void OnEntry()
            {
                (this.Machine as ClientMachine).Server = (Id)this.Payload;
                (this.Machine as ClientMachine).Counter = 0;
                this.Raise(new Unit());
            }
        }

        [OnEventGotoState(typeof(Unit), typeof(Active))]
        [OnEventDoAction(typeof(Pong), nameof(SendPing))]
        class Active : MachineState
        {
            protected override void OnEntry()
            {
                if ((this.Machine as ClientMachine).Counter == 5)
                {
                    this.Raise(new Halt());
                }
            }
        }

        private void SendPing()
        {
            this.Counter++;
            Console.WriteLine("\nTurns: {0} / 5\n", this.Counter);
            this.Send(this.Server, new Ping());
            this.Raise(new Unit());
        }
    }
}
