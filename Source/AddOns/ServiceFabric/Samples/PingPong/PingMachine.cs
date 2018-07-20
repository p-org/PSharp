using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.Utilities;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace PingPong
{
    class PingMachine : ReliableMachine
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
            var pongMachineId = this.CreateMachine(typeof(PongMachine), new PongEvent(this.Id));
            await PongMachine.Set(pongMachineId);
            this.Goto<Waiting>();
        }

        private async Task Reply()
        {
            var cnt = await Count.Get();
            if (cnt < 5)
            {
                Send(await PongMachine.Get(), new PongEvent(this.Id));
                await Count.Set(cnt + 1);
            }
        }

        protected override Task OnActivate()
        {
            Count = this.GetOrAddRegister<int>("Count", 0);
            PongMachine = this.GetOrAddRegister<MachineId>("PongMachine", null);
            return Task.CompletedTask;
        }
    }
}
