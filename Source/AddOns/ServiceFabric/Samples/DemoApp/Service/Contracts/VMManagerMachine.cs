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
        [OnEntry(nameof(CreateVM))]
        [OnEventDoAction(typeof(eVMCreateRequestEvent), nameof(CreateVM))]
        [OnEventGotoState(typeof(eVMCreateSuccessRequestEvent), typeof(Created))]
        [OnEventGotoState(typeof(eVMDeleteRequestEvent), typeof(Deleting))]
        class Creating : MachineState
        {
        }

        [OnEntry(nameof(DeleteVM))]
        [OnEventDoAction(typeof(eVMDeleteRequestEvent), nameof(DeleteVM))]
        class Deleting : MachineState
        {
        }

        [OnEntry(nameof(OnCreateCompleted))]
        [OnEventGotoState(typeof(eVMDeleteRequestEvent), typeof(Deleting))]
        class Created : MachineState
        {
        }

        private async Task CreateVM()
        {
            eVMCreateRequestEvent request = this.ReceivedEvent as eVMCreateRequestEvent;
            if (request == null) return;
            this.Monitor<LivenessMonitor>(new LivenessMonitor.eVmManagerMachineUp());

            if (this.FairRandom())
            {
                this.Logger.WriteLine($"VM- {this.Id} Creating request failed request for pool {request.senderId}");
                this.Send(request.senderId, new eVMCreateFailureRequestEvent(this.Id));
            }
            else
            {
                this.Logger.WriteLine($"VM- {this.Id} Creating request success for pool {request.senderId}");
                this.Send(this.Id, new eVMCreateSuccessRequestEvent(this.Id));
                this.Send(request.senderId, new eVMCreateSuccessRequestEvent(this.Id));
            }
            await Task.Yield();
        }

        private async Task DeleteVM()
        {
            eVMDeleteRequestEvent request = this.ReceivedEvent as eVMDeleteRequestEvent;
            
            if (this.FairRandom())
            {
                this.Logger.WriteLine($"VM- {this.Id} Deleting request failed request for pool {request.senderId}");
                this.Send(request.senderId, new eVMDeleteFailureRequestEvent(this.Id));
            }
            else
            {
                this.Logger.WriteLine($"VM- {this.Id} Deleting request success for pool {request.senderId}");
                this.Send(request.senderId, new eVMDeleteSuccessRequestEvent(this.Id));
                this.Send(this.Id, new Halt());
                this.Monitor<LivenessMonitor>(new LivenessMonitor.eVmManagerMachineDown());
            }
            await Task.Yield();
        }

        private void OnCreateCompleted()
        {
            eVMCreateSuccessRequestEvent request = this.ReceivedEvent as eVMCreateSuccessRequestEvent;
            this.Logger.WriteLine($"VM- {this.Id} Created state success for pool {request.senderId}");
        }
    }
}
