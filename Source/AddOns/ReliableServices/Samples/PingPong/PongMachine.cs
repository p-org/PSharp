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
    class PongMachine : ReliableStateMachine
    {
        [Start]
        [OnEntry(nameof(Reply))]
        [OnEventDoAction(typeof(PongEvent), nameof(Reply))]
        class Init : MachineState { }

        private async Task Reply()
        {
            var sender = (this.ReceivedEvent as PongEvent).PingMachineId;

            await this.ReliableSend(sender, new PingEvent());
        }

        protected override Task OnActivate()
        {
            return Task.CompletedTask;
        }
    }
}
