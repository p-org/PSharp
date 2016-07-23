using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Client : Machine
    {
        private MachineId Server;
        private int Counter;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

        void Configure()
        {
            this.Server = (this.ReceivedEvent as Config).Id;
            this.Counter = 0;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(ActiveOnEntry))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            while (this.Counter < 5)
            {
                this.Receive(typeof(Ping));
                this.SendPong();
            }

            this.Raise(new Halt());
        }

        private void SendPong()
        {
            this.Counter++;
            Console.WriteLine("\nTurns: {0} / 5\n", this.Counter);
            this.Send(this.Server, new Pong());
        }
    }
}
