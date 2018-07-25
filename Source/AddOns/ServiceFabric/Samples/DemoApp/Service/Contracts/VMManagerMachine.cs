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
        [OnEventDoAction(typeof(eVMRetryCreateRequestEvent), nameof(CreateVM))]
        [OnEventGotoState(typeof(eVMCreateSuccessRequestEvent), typeof(Created))]
        [OnEventGotoState(typeof(eVMDeleteRequestEvent), typeof(Deleting))]
        class Creating : MachineState
        {
        }

        [OnEntry(nameof(DeleteVM))]
        [OnEventGotoState(typeof(eVMRetryDeleteRequestEvent), typeof(RetryDeleting))]
        class Deleting : MachineState
        {
        }

        [OnEntry(nameof(OnCreateCompleted))]
        [OnEventGotoState(typeof(eVMDeleteRequestEvent), typeof(Deleting))]
        class Created : MachineState
        {
        }

        [OnEntry(nameof(RetryDeleteVM))]
        class RetryDeleting : MachineState
        {
        }

        private async Task RetryCreateVM(Event receivedEvent)
        {
            eVMRetryCreateRequestEvent request = receivedEvent as eVMRetryCreateRequestEvent;
            this.Logger.WriteLine($"VM- {this.Id} Retry create request success for pool {request.senderId}");
            this.Send(this.Id, new eVMCreateSuccessRequestEvent(this.Id));
            await Task.Yield();
        }

        private async Task RetryDeleteVM()
        {
            eVMRetryDeleteRequestEvent request = this.ReceivedEvent as eVMRetryDeleteRequestEvent;
            this.Logger.WriteLine($"VM- {this.Id} Retry delete request success for pool {request.senderId}");
            this.Send(this.Id, new Halt());
            await Task.Yield();
        }

        private async Task CreateVM()
        {
            eVMCreateRequestEvent request = this.ReceivedEvent as eVMCreateRequestEvent;
            if(request == null)
            {
                await RetryCreateVM(this.ReceivedEvent);
                return;
            }

            System.Random randomInteger = new System.Random();
            if (randomInteger.Next() % 2 == 0)
            {
                this.Logger.WriteLine($"VM- {this.Id} Creating request failed request for pool {request.senderId}");
                this.Send(request.senderId, new eVMCreateFailureRequestEvent(this.Id));
            }
            else
            {
                this.Logger.WriteLine($"VM- {this.Id} Creating request success for pool {request.senderId}");
                this.Send(this.Id, new eVMCreateSuccessRequestEvent(this.Id));
            }
            await Task.Yield();
        }

        private async Task DeleteVM()
        {
            eVMDeleteRequestEvent request = this.ReceivedEvent as eVMDeleteRequestEvent;

            System.Random randomInteger = new System.Random();
            if (randomInteger.Next() % 2 == 0)
            {
                this.Logger.WriteLine($"VM- {this.Id} Deleting request failed request for pool {request.senderId}");
                this.Send(request.senderId, new eVMDeleteFailureRequestEvent(this.Id));
            }
            else
            {
                this.Logger.WriteLine($"VM- {this.Id} Deleting request success for pool {request.senderId}");
                this.Send(this.Id, new Halt());
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
