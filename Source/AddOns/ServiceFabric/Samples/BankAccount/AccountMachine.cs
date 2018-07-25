using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.Utilities;
using Microsoft.ServiceFabric.Data;

namespace BankAccount
{
    /// <summary>
    /// A single users' bank account.
    /// </summary>
    class AccountMachine : ReliableMachine
    {
        /// <summary>
        /// Name of the owner.
        /// </summary>
        ReliableRegister<string> OwnerName;

        /// <summary>
        /// Account balance.
        /// </summary>
        ReliableRegister<int> Balance;

        /// <summary>
        /// Account enabled.
        /// </summary>
        ReliableRegister<bool> Enabled;

        public AccountMachine(IReliableStateManager stateManager)
           : base(stateManager)
        { }

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
            await OwnerName.Set(ev.Name);
            await Balance.Set(ev.Balance);

            this.Logger.WriteLine("Account {0} started with balance {1}", ev.Name, ev.Balance);

            this.Goto<WaitForOp>();
        }

        private async Task DoEnable()
        {
            this.Logger.WriteLine("Account {0} enabled", await OwnerName.Get());
            await Enabled.Set(true);
        }

        private async Task DoDeposit()
        {
            var ev = (this.ReceivedEvent as DepositEvent);
            var enabled = await Enabled.Get();

            if (!enabled)
            {
                this.Logger.WriteLine("Account {0}: Deposit of {1} failed because account is disabled", await OwnerName.Get(), ev.amount);
                Send(ev.sender, new FailureEvent());
                return;
            }

            var value = await Balance.Get() + ev.amount;
            this.Monitor<SafetyMonitor>(new AccountBalanceUpdatedEvent(this.Id, value));
            await Balance.Set(value);
            this.Logger.WriteLine("Account {0}: Deposited {1}, current balance {2}", await OwnerName.Get(), ev.amount, value);
            Send(ev.sender, new SuccessEvent());
        }

        private async Task DoWithdraw()
        {
            var ev = (this.ReceivedEvent as WithdrawEvent);
            var enabled = await Enabled.Get();

            if (!enabled)
            {
                this.Logger.WriteLine("Account {0}: Withdraw of {1} failed because account is disabled", await OwnerName.Get(), ev.amount);
                Send(ev.sender, new FailureEvent());
                return;
            }

            var balance = await Balance.Get();
            if (balance < ev.amount)
            {
                this.Logger.WriteLine("Account {0}: Withdraw of {1} failed, insufficient balance", await OwnerName.Get(), ev.amount);
                Send(ev.sender, new FailureEvent());
                return;
            }

            var value = balance - ev.amount;
            this.Monitor<SafetyMonitor>(new AccountBalanceUpdatedEvent(this.Id, value));
            await Balance.Set(value);
            this.Logger.WriteLine("Account {0}: Withdrew {1}, current balance {2}", await OwnerName.Get(), ev.amount, value);
            Send(ev.sender, new SuccessEvent());
        }

        protected override Task OnActivate()
        {
            OwnerName = this.GetOrAddRegister<string>("Owner", "Null");
            Balance = this.GetOrAddRegister<int>("Balance", 0);
            Enabled = this.GetOrAddRegister<bool>("Enabled", false);
            return Task.CompletedTask;
        }
    }
}
