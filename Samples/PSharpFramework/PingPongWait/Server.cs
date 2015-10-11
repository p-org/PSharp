using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Server : Machine
    {
        MachineId Client;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Client = this.CreateMachine(typeof(Client), this.Id);
            this.Raise(new Unit());
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(Pong), nameof(SendPing))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.SendPing();
        }

        void SendPing()
        {
            this.Send(this.Client, new Ping());
        }
    }
}
