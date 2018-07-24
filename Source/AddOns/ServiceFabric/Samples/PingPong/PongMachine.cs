using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.ServiceFabric.Data;

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
            this.Logger.WriteLine($"{this.Id} - activating...");
            return Task.CompletedTask;
        }
    }
}
