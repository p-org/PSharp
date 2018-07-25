﻿using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.Utilities;
using Microsoft.ServiceFabric.Data;

namespace PingPong
{
    [DataContract]
    public class PingEvent : Event { }

    public class PingMachine : ReliableMachine
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PingMachine(IReliableStateManager stateManager)
            : base(stateManager)
        { }

        ReliableRegister<int> Count;
        ReliableRegister<MachineId> PongMachine;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        [OnEventDoAction(typeof(PingEvent), nameof(Reply))]
        class Waiting : MachineState { }

        private async Task InitOnEntry()
        {
            try
            {
                var pongMachineId = this.CreateMachine(typeof(PongMachine), new PongEvent(this.Id));
                this.Monitor<LivenessMonitor>(new LivenessMonitor.CheckPongEvent());
                await PongMachine.Set(pongMachineId);
                this.Goto<Waiting>();
            }
            catch (Exception ex)
            {
                this.Logger.WriteLine($"{ex}");
                throw ex;
            }
        }

        private async Task Reply()
        {
            int count = (await Count.Get()) + 1;
            if (count <= 2)
            {
                Send(await PongMachine.Get(), new PongEvent(this.Id));
                this.Monitor<LivenessMonitor>(new LivenessMonitor.CheckPongEvent());
                this.Monitor<SafetyMonitor>(new SafetyMonitor.CheckReplyCount(count));
                await Count.Set(count);
                this.Logger.WriteLine("#Pings: {0} / 2", count);
            }

            //Send(await PongMachine.Get(), new Halt());
            //var pongMachineId = this.CreateMachine(typeof(PongMachine), new PongEvent(this.Id));
            //await PongMachine.Set(pongMachineId);
        }

        protected override Task OnActivate()
        {
            this.Logger.WriteLine($"{this.Id} - activating...");
            Count = this.GetOrAddRegister<int>("Count", 0);
            PongMachine = this.GetOrAddRegister<MachineId>("PongMachine", null);
            return Task.CompletedTask;
        }
    }
}
