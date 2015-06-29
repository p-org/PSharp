using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Client : Machine
    {
        private MachineId Server;
        private int Counter;

        [Initial]
        [OnEventGotoState(typeof(Unit), typeof(Playing))]
        class Init : MachineState
        {
            protected override void OnEntry()
            {
                (this.Machine as Client).Server = (MachineId)this.Payload;
                (this.Machine as Client).Counter = 0;
                this.Raise(new Unit());
            }
        }

        [OnEventGotoState(typeof(Unit), typeof(Playing))]
        [OnEventDoAction(typeof(Pong), nameof(SendPing))]
        class Playing : MachineState
        {
            protected override void OnEntry()
            {
                if ((this.Machine as Client).Counter == 5)
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
