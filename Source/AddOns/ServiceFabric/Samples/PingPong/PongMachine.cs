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
    public class PongMachine : ReliableMachine
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public PongMachine(IReliableStateManager stateManager)
            : base(stateManager)
        { }

        [Start]
        [OnEntry(nameof(Reply))]
        [OnEventDoAction(typeof(PongEvent), nameof(Reply))]
        class Init : MachineState { }

        private void Reply()
        {
            var sender = (this.ReceivedEvent as PongEvent).PingMachineId;
            Send(sender, new PingEvent());
        }

        protected override Task OnActivate()
        {
            return Task.CompletedTask;
        }
    }
}
