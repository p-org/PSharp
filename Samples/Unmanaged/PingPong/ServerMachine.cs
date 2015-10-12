using System;
using Microsoft.PSharp;

using PingPongWrapper;

namespace PingPong
{
    internal class ServerMachine : Machine
    {
        ServerWrapper Server;
        MachineId Client;

		[Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Unit), typeof(Active))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Client = this.CreateMachine(typeof(ClientMachine));
            this.Send(this.Client, new Events.ConfigEvent(this.Id));
            
            this.Server = new ServerWrapper(this.Client);

            this.Raise(new Unit());
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventDoAction(typeof(Events.MessageEvent), nameof(SendPong))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.Send(this.Client, new Events.MessageEvent());
        }

        void SendPong()
        {
            this.Server.invoke(new Unit());
        }
    }
}
