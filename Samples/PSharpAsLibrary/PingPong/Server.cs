using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Server<T> : Machine
    {
        MachineId Client;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Client = this.CreateMachine(typeof(Client), "TheUltimateClientMachine");
            this.Send(this.Client, new Config(this.Id));
            this.Goto(typeof(Active<int>));
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(Ping), nameof(SendPong))]
        class Active<N> : MachineState { }

        void ActiveOnEntry()
        {
            this.SendPong();
        }

        public void SendPong()
        {
            this.Send(this.Client, new Pong());
        }
    }
}
