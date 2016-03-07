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
            this.Client = this.CreateRemoteMachine(typeof(Client),
                "localhost:8000", new Config(this.Id));
            this.Raise(new Unit());
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
            this.RemoteSend(this.Client, new Pong());
        }
    }
}
