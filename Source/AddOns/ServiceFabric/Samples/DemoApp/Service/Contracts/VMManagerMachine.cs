namespace PoolServicesContract
{
    using System.Threading.Tasks;
    using Microsoft.PSharp;
    using Microsoft.PSharp.ServiceFabric;
    using Microsoft.ServiceFabric.Data;

    public class VMManagerMachine : ReliableMachine
    {
        public VMManagerMachine(IReliableStateManager stateManager) : base(stateManager)
        {
        }

        protected override Task OnActivate()
        {
            return Task.FromResult(true);
        }

        [Start]
        [OnEntry(nameof(CreatePool))]
        [OnEventDoAction(typeof(eVMCreateRequestEvent), nameof(CreatePool))]
        [OnEventGotoState(typeof(ePoolDeletionRequestEvent), typeof(Deleting))]
        class Resizing : MachineState
        {
        }

        [OnEntry(nameof(DeletePool))]
        class Deleting : MachineState
        {
        }

        private async Task CreatePool()
        {
            eVMCreateRequestEvent resizeRequest = this.ReceivedEvent as eVMCreateRequestEvent;
            
            this.Logger.WriteLine($"VMManagerMachine Resize request for pool {resizeRequest.senderId}");
            await Task.Yield();
        }

        private async Task DeletePool()
        {
            this.Logger.WriteLine($"Deletion request of pool {this.Id}");
            this.Send(this.Id, new Halt());
            await Task.Yield();
        }
    }
}
