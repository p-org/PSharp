using System;
using Microsoft.PSharp;

namespace PingPong
{
    internal class Server : Machine
    {
        MachineId Client;

		[Initial]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Playing))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Client = this.CreateMachine(typeof(Client), this.Id);
            this.Raise(new Unit());
        }

		[OnEventDoAction(typeof(Unit), nameof(SendPong))]
        [OnEventDoAction(typeof(Ping), nameof(SendPong))]
        class Playing : MachineState
        {
            protected override void OnEntry()
            {
                this.Send((this.Machine as Server).Client, new Pong());
            }
        }

        void SendPong()
        {
            this.Send(this.Client, new Pong());
        }
    }
}
