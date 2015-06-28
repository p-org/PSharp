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
        [OnEventGotoState(typeof(Unit), typeof(Playing))]
        [DeferEvents(typeof(Unit))]
        [IgnoreEvents(typeof(Unit), typeof(Ping))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            new Client();
            this.Client = this.CreateMachine(typeof(Client), this).Models(typeof(Client));
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
