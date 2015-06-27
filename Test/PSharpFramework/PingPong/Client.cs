using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Client : Machine
    {
        private MachineId Server;
        private int Counter;

        [Initial]
        class Init : MachineState
        {
            protected override void OnEntry()
            {
                (this.Machine as Client).Server = (MachineId)this.Payload;
                (this.Machine as Client).Counter = 0;
                this.Raise(new Unit());
            }

            //on Unit goto Playing;
        }

        class Playing : MachineState
        {
            protected override void OnEntry()
            {
                if ((this.Machine as Client).Counter == 5)
                {
                    this.Raise(new Halt());
                }
            }

            //on Unit goto Playing;
            //on Pong do SendPing;
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
