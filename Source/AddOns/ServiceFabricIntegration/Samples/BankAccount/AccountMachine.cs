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
    /// <summary>
    /// A single users' bank account
    /// </summary>
    class AccountMachine : ReliableStateMachine
    {
        /// <summary>
        /// Name of the owner
        /// </summary>
        ReliableRegister<string> OwnerName;

        /// <summary>
        /// Account balance
        /// </summary>
        ReliableRegister<int> Balance;

        /// <summary>
        /// Account enabled
        /// </summary>
        ReliableRegister<bool> Enabled;

        /// <param name="stateManager"></param>
        public AccountMachine(IReliableStateManager stateManager)
            : base(stateManager) { }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        [OnEventDoAction(typeof(EnableEvent), nameof(DoEnable))]
        [OnEventDoAction(typeof(WithdrawEvent), nameof(DoWithdraw))]
        [OnEventDoAction(typeof(DepositEvent), nameof(DoDeposit))]
        class WaitForOp : MachineState { }


        private async Task InitOnEntry()
        {
            var ev = (this.ReceivedEvent as InitializeAccountEvent);
            await OwnerName.Set(CurrentTransaction, ev.Name);
            await Balance.Set(CurrentTransaction, ev.Balance);

            this.Logger.WriteLine("Account {0} started with balance {1}", ev.Name, ev.Balance);

            this.Goto<WaitForOp>();
        }

        private async Task DoEnable()
        {
            this.Logger.WriteLine("Account {0} enabled", await OwnerName.Get(CurrentTransaction));
            await Enabled.Set(CurrentTransaction, true);
        }

        private async Task DoDeposit()
        {
            var ev = (this.ReceivedEvent as DepositEvent);
            var enabled = await Enabled.Get(CurrentTransaction);

            if(!enabled)
            {
                this.Logger.WriteLine("Account {0}: Deposit of {1} failed because account is disabled", await OwnerName.Get(CurrentTransaction), ev.amount);
                await this.ReliableSend(ev.sender, new FailureEvent());
                return;
            }

            var value = await Balance.Get(CurrentTransaction) + ev.amount;
            this.Monitor<SafetyMonitor>(new AccountBalanceUpdatedEvent(this.Id, value));
            await Balance.Set(CurrentTransaction, value);
            this.Logger.WriteLine("Account {0}: Deposited {1}, current balance {2}", await OwnerName.Get(CurrentTransaction), ev.amount, value);
            await this.ReliableSend(ev.sender, new SuccessEvent());
        }


        private async Task DoWithdraw()
        {
            var ev = (this.ReceivedEvent as WithdrawEvent);
            var enabled = await Enabled.Get(CurrentTransaction);

            if (!enabled)
            {
                this.Logger.WriteLine("Account {0}: Withdraw of {1} failed because account is disabled", await OwnerName.Get(CurrentTransaction), ev.amount);
                await this.ReliableSend(ev.sender, new FailureEvent());
                return;
            }

            var balance = await Balance.Get(CurrentTransaction);
            if(balance < ev.amount)
            {
                this.Logger.WriteLine("Account {0}: Withdraw of {1} failed, insufficient balance", await OwnerName.Get(CurrentTransaction), ev.amount);
                await this.ReliableSend(ev.sender, new FailureEvent());
                return;
            }

            var value = balance - ev.amount;
            this.Monitor<SafetyMonitor>(new AccountBalanceUpdatedEvent(this.Id, value));
            await Balance.Set(CurrentTransaction, value);
            this.Logger.WriteLine("Account {0}: Withdrew {1}, current balance {2}", await OwnerName.Get(CurrentTransaction), ev.amount, value);
            await this.ReliableSend(ev.sender, new SuccessEvent());
        }

        /// <summary>
        /// (Re-)Initialize
        /// </summary>
        /// <returns></returns>
        public override Task OnActivate()
        {
            OwnerName = new ReliableRegister<string>(QualifyWithMachineName("Owner"), StateManager, "Null");
            Balance = new ReliableRegister<int>(QualifyWithMachineName("Balance"), StateManager, 0);
            Enabled = new ReliableRegister<bool>(QualifyWithMachineName("Enabled"), StateManager, false);
            return Task.CompletedTask;
        }

        private string QualifyWithMachineName(string name)
        {
            return name + "_" + this.Id.Name;
        }
    }
}
