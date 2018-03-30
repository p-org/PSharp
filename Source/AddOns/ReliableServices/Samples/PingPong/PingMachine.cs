using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Utilities;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace PingPong
{
    class PingMachine : ReliableStateMachine
    {
        ReliableRegister<int> Count;
        ReliableRegister<IRsmId> PongMachine;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        [OnEventDoAction(typeof(PingEvent), nameof(Reply))]
        class Waiting : MachineState { }

        private async Task InitOnEntry()
        {
            var pongMachineId = await this.Host.ReliableCreateMachine<PongMachine>(new PongEvent(this.Host.Id));
            await PongMachine.Set(pongMachineId);
            this.Goto<Waiting>();
        }

        private async Task Reply()
        {
            var cnt = await Count.Get();
            if (cnt < 5)
            {
                await this.Host.ReliableSend(await PongMachine.Get(), new PongEvent(this.Host.Id));
                await Count.Set(cnt + 1);
            }
        }

        protected override Task OnActivate()
        {
            Count = this.Host.GetOrAddRegister<int>("Count", 0);
            PongMachine = this.Host.GetOrAddRegister<IRsmId>("PongMachine", null);
            return Task.CompletedTask;
        }
    }
}
