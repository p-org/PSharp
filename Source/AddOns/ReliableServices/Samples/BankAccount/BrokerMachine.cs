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
        ReliableRegister<IRsmId> Client;

        /// <summary>
        /// Source account
        /// </summary>
        ReliableRegister<IRsmId> Source;

        /// <summary>
        /// Target account
        /// </summary>
        ReliableRegister<IRsmId> Target;

        /// <summary>
        /// Transfer amount
        /// </summary>
        ReliableRegister<int> TransferAmount;

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
            await this.ReliableSend(ev.Source, new WithdrawEvent(this.ReliableId, ev.Amount));
            this.Goto<WaitForWithdraw>();
        }

        private async Task OnWithdrawFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting");
            var client = await Client.Get();
            if (client != null)
            {
                await this.ReliableSend(client, new AbortedEvent());
            }

            this.Raise(new Halt());
        }

        private async Task OnWithdrawSuccess()
        {
            await this.ReliableSend(await Target.Get(), new DepositEvent(this.ReliableId, await TransferAmount.Get()));
            this.Goto<WaitForDeposit>();
        }

        private async Task OnDepositFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting with undo");
            await this.ReliableSend(await Source.Get(), new DepositEvent(this.ReliableId, await TransferAmount.Get()));
            this.Goto<Compensate>();
        }

        private async Task OnDepositSuccess()
        {
            this.Logger.WriteLine("Transfer operation completed.");
            var client = await Client.Get();
            if (client != null)
            {
                await this.ReliableSend(client, new SuccessEvent());
            }
            this.Raise(new Halt());
        }

        private async Task OnCompensateFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting with undo");
            var client = await Client.Get();
            if (client != null)
            {
                await this.ReliableSend(client, new FailureEvent());
            }
            this.Goto<Compensate>();
        }

        private async Task OnCompensateSuccess()
        {
            this.Logger.WriteLine("Transfer operation aborted successfully.");
            var client = await Client.Get();
            if (client != null)
            {
                await this.ReliableSend(client, new AbortedEvent());
            }
            this.Raise(new Halt());
        }

        /// <summary>
        /// (Re-)Initialize
        /// </summary>
        /// <returns></returns>
        protected override Task OnActivate()
        {
            Client = this.Host.GetOrAddRegister<IRsmId>("Client");
            Source = this.Host.GetOrAddRegister<IRsmId>("Source");
            Target = this.Host.GetOrAddRegister<IRsmId>("Target");
            TransferAmount = this.Host.GetOrAddRegister<int>("Amount");
            return Task.CompletedTask;
        }

    }
}
