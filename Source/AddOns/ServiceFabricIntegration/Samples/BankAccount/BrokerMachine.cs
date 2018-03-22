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

namespace BankAccount
{
    class BrokerMachine : ReliableStateMachine
    {
        /// <summary>
        /// Client
        /// </summary>
        ReliableRegister<MachineId> Client;

        /// <summary>
        /// Source account
        /// </summary>
        ReliableRegister<MachineId> Source;

        /// <summary>
        /// Target account
        /// </summary>
        ReliableRegister<MachineId> Target;

        /// <summary>
        /// Transfer amount
        /// </summary>
        ReliableRegister<int> TransferAmount;

        /// <param name="stateManager"></param>
        public BrokerMachine(IReliableStateManager stateManager)
            : base(stateManager) { }


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
            await Client.Set(CurrentTransaction, ev.Client);
            await Source.Set(CurrentTransaction, ev.Source);
            await Target.Set(CurrentTransaction, ev.Target);
            await TransferAmount.Set(CurrentTransaction, ev.Amount);

            // withdraw
            await this.ReliableSend(ev.Source, new WithdrawEvent(this.Id, ev.Amount));
            this.Goto<WaitForWithdraw>();
        }

        private async Task OnWithdrawFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting");
            await this.ReliableSend(await Client.Get(CurrentTransaction), new AbortedEvent());
            this.Raise(new Halt());
        }

        private async Task OnWithdrawSuccess()
        {
            await this.ReliableSend(await Target.Get(CurrentTransaction), new DepositEvent(this.Id, await TransferAmount.Get(CurrentTransaction)));
            this.Goto<WaitForDeposit>();
        }

        private async Task OnDepositFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting with undo");
            await this.ReliableSend(await Source.Get(CurrentTransaction), new DepositEvent(this.Id, await TransferAmount.Get(CurrentTransaction)));
            this.Goto<Compensate>();
        }

        private async Task OnDepositSuccess()
        {
            this.Logger.WriteLine("Transfer operation completed.");
            await this.ReliableSend(await Client.Get(CurrentTransaction), new SuccessEvent());
            this.Raise(new Halt());
        }

        private async Task OnCompensateFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting with undo");
            await this.ReliableSend(await Client.Get(CurrentTransaction), new FailureEvent());
            this.Goto<Compensate>();
        }

        private async Task OnCompensateSuccess()
        {
            this.Logger.WriteLine("Transfer operation aborted successfully.");
            await this.ReliableSend(await Client.Get(CurrentTransaction), new AbortedEvent());
            this.Raise(new Halt());
        }

        /// <summary>
        /// (Re-)Initialize
        /// </summary>
        /// <returns></returns>
        public override Task OnActivate()
        {
            Client = new ReliableRegister<MachineId>(QualifyWithMachineName("Client"), StateManager);
            Source = new ReliableRegister<MachineId>(QualifyWithMachineName("Source"), StateManager);
            Target = new ReliableRegister<MachineId>(QualifyWithMachineName("Target"), StateManager);
            TransferAmount = new ReliableRegister<int>(QualifyWithMachineName("Amount"), StateManager);
            return Task.CompletedTask;
        }

        private string QualifyWithMachineName(string name)
        {
            return name + "_" + this.Id.Name;
        }
    }
}
