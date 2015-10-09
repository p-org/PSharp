using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Client : Machine
    {
        private Id Server;
        private int Counter;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Server = (Id)this.Payload;
            this.Counter = 0;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(ActiveOnEntry))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            while (this.Counter < 5)
            {
                this.Receive(new Tuple<Type, Action>(typeof(Ping), () =>
                {
                    this.SendPong();
                }));
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
