using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Server : Machine
    {
        MachineId Client;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Client = this.CreateMachine(typeof(Client));
            this.Send(this.Client, new Config(this.Id));
            this.Goto(typeof(Active));
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(Ping), nameof(SendPong))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.SendPong();
        }

        void SendPong()
        {
            this.Send(this.Client, new Pong());
        }
    }
}
