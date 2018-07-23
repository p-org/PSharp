namespace PoolServicesContract
{
    using System.Threading.Tasks;
    using Microsoft.PSharp;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;

    public class PoolManagerMachine : ReliableMachine
    {
        public PoolManagerMachine(IReliableStateManager stateManager) : base(stateManager)
        {
        }

        protected override Task OnActivate()
        {
            return Task.FromResult(true);
        }

        [Start]
        [OnEntry(nameof(ResizePool))]
        [OnEventDoAction(typeof(ePoolResizeRequest), nameof(ResizePool))]
        [OnEventGotoState(typeof(ePoolDeletionRequest), typeof(Deleting))]
        class Resizing : MachineState
        {
        }

        [OnEntry(nameof(DeletePool))]
        class Deleting : MachineState
        {
        }

        private async Task ResizePool()
        {
            ePoolResizeRequest resizeRequest = this.ReceivedEvent as ePoolResizeRequest;
            this.Logger.WriteLine($"Resize request for pool {this.Id} and size {resizeRequest.Size}");
        }

        private async Task DeletePool()
        {
            this.Logger.WriteLine($"Deletion request of pool {this.Id}");
            this.Send(this.Id, new Halt());
        }
    }
}
