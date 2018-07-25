using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.Utilities;
using Microsoft.ServiceFabric.Data;

namespace BankAccount
{
    class BrokerMachine : ReliableMachine
    {
        /// <summary>
        /// Client.
        /// </summary>
        ReliableRegister<MachineId> Client;

        /// <summary>
        /// Source account.
        /// </summary>
        ReliableRegister<MachineId> Source;

        /// <summary>
        /// Target account.
        /// </summary>
        ReliableRegister<MachineId> Target;

        /// <summary>
        /// Transfer amount.
        /// </summary>
        ReliableRegister<int> TransferAmount;

        public BrokerMachine(IReliableStateManager stateManager)
           : base(stateManager)
        { }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        [OnEventDoAction(typeof(FailureEvent), nameof(OnWithdrawFailure))]
        [OnEventDoAction(typeof(SuccessEvent), nameof(OnWithdrawSuccess))]
        class WaitForWithdraw : MachineState { }

        [OnEventDoAction(typeof(FailureEvent), nameof(OnDepositFailure))]
        [OnEventDoAction(typeof(SuccessEvent), nameof(OnDepositSuccess))]
        class WaitForDeposit : MachineState { }

        [OnEventDoAction(typeof(FailureEvent), nameof(OnCompensateFailure))]
        [OnEventDoAction(typeof(SuccessEvent), nameof(OnCompensateSuccess))]
        class Compensate : MachineState { }

        private async Task InitOnEntry()
        {
            var ev = (this.ReceivedEvent as InitializeBrokerEvent);
            await Client.Set(ev.Client);
            await Source.Set(ev.Source);
            await Target.Set(ev.Target);
            await TransferAmount.Set(ev.Amount);

            // withdraw
            Send(ev.Source, new WithdrawEvent(this.Id, ev.Amount));
            this.Goto<WaitForWithdraw>();
        }

        private async Task OnWithdrawFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting");
            var client = await Client.Get();
            if (client != null)
            {
                Send(client, new AbortedEvent());
            }

            this.Raise(new Halt());
        }

        private async Task OnWithdrawSuccess()
        {
            Send(await Target.Get(), new DepositEvent(this.Id, await TransferAmount.Get()));
            this.Goto<WaitForDeposit>();
        }

        private async Task OnDepositFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting with undo");
            Send(await Source.Get(), new DepositEvent(this.Id, await TransferAmount.Get()));
            this.Goto<Compensate>();
        }

        private async Task OnDepositSuccess()
        {
            this.Logger.WriteLine("Transfer operation completed.");
            var client = await Client.Get();
            if (client != null)
            {
                Send(client, new SuccessEvent());
            }
            this.Raise(new Halt());
        }

        private async Task OnCompensateFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting with undo");
            var client = await Client.Get();
            if (client != null)
            {
                Send(client, new FailureEvent());
            }
            this.Goto<Compensate>();
        }

        private async Task OnCompensateSuccess()
        {
            this.Logger.WriteLine("Transfer operation aborted successfully.");
            var client = await Client.Get();
            if (client != null)
            {
                Send(client, new AbortedEvent());
            }
            this.Raise(new Halt());
        }

        protected override Task OnActivate()
        {
            Client = this.GetOrAddRegister<MachineId>("Client");
            Source = this.GetOrAddRegister<MachineId>("Source");
            Target = this.GetOrAddRegister<MachineId>("Target");
            TransferAmount = this.GetOrAddRegister<int>("Amount");
            return Task.CompletedTask;
        }
    }
}
