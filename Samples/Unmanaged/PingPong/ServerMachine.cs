using System;
using Microsoft.PSharp;
using Microsoft.PSharp.Interop;

using PingPongWrapper;

namespace PingPong
{
    internal class ServerMachine : Machine
    {
        ServerWrapper Server;
        Id Client;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Server = new ServerWrapper();
            this.Server.invoke(new Unit());
            this.Client = this.CreateMachine(typeof(ClientMachine), this.Id);
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
            this.Send(this.Client, new Pong());
        }
    }
}
