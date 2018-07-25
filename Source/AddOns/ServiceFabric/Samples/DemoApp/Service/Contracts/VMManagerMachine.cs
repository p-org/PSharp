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
        [OnEventGotoState(typeof(eVMDeleteRequestEvent), typeof(Deleting))]
        [OnEventGotoState(typeof(eVMRenewRequestEvent), typeof(Recreating))]
        class Creating : MachineState
        {
        }

        [OnEntry(nameof(DeletePool))]
        class Deleting : MachineState
        {
        }

        [OnEntry(nameof(RecreateVM))]
        [OnEventGotoState(typeof(eVMDeleteRequestEvent), typeof(Deleting))]
        class Recreating : MachineState
        {
        }

        private async Task RecreateVM()
        {
            eVMRenewRequestEvent createRequest = this.ReceivedEvent as eVMRenewRequestEvent;
            this.Logger.WriteLine($"VMManagerMachine Re create vm request success for pool {createRequest.senderId}");
            await Task.Yield();
        }

        private async Task CreatePool()
        {
            eVMCreateRequestEvent createRequest = this.ReceivedEvent as eVMCreateRequestEvent;
            
            System.Random randomInteger = new System.Random();
            if (randomInteger.Next() % 2 == 0)
            {
                this.Logger.WriteLine($"VMManagerMachine Create request failed request for pool {createRequest.senderId}");
                this.Send(createRequest.senderId, new eVMFailureEvent(this.Id));
            }
            else
            {
                this.Logger.WriteLine($"VMManagerMachine Create request success for pool {createRequest.senderId}");
            }
            await Task.Yield();
        }

        private async Task DeletePool()
        {
            eVMDeleteRequestEvent deleteRequest = this.ReceivedEvent as eVMDeleteRequestEvent;
            this.Logger.WriteLine($"VMManagerMachine Deletion request of vm {this.Id} for pool {deleteRequest.senderId}");
            this.Send(this.Id, new Halt());
            await Task.Yield();
        }
    }
}
