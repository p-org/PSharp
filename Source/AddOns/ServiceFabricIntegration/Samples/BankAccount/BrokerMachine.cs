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
        class Init : MachineState { }

        [OnEventDoAction(typeof(FailureEvent), nameof(OnWithdrawFailure))]
        [OnEventDoAction(typeof(SuccessEvent), nameof(OnWithdrawSuccess))]
        class WaitForWithdraw : MachineState { }

        [OnEventDoAction(typeof(FailureEvent), nameof(OnDepositFailure))]
        [OnEventDoAction(typeof(SuccessEvent), nameof(OnDepositSuccess))]
        class WaitForDeposit : MachineState { }

        class Compensate : MachineState { }

        private async Task InitOnEntry()
        {
            var ev = (this.ReceivedEvent as InitializeBrokerEvent);
            await Source.Set(CurrentTransaction, ev.Source);
            await Target.Set(CurrentTransaction, ev.Target);
            await TransferAmount.Set(CurrentTransaction, ev.Amount);

            // withdraw
            await this.ReliableSend(ev.Source, new WithdrawEvent(this.Id, ev.Amount));
            this.Goto<WaitForWithdraw>();
        }

        private void OnWithdrawFailure()
        {
            this.Logger.WriteLine("Transfer operation failed. Aborting");
            this.Raise(new Halt());
        }

        private async Task OnWithdrawSuccess()
        {
            await this.ReliableSend(await Target.Get(CurrentTransaction), new DepositEvent(this.Id, await TransferAmount.Get(CurrentTransaction)));
            this.Goto<WaitForDeposit>();
        }

        /// <summary>
        /// (Re-)Initialize
        /// </summary>
        /// <returns></returns>
        public override Task OnActivate()
        {
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
