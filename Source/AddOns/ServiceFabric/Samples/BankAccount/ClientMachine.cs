using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ServiceFabric;
using Microsoft.PSharp.ServiceFabric.Utilities;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace BankAccount
{
    class ClientMachine : ReliableMachine
    {
        public ClientMachine(IReliableStateManager stateManager)
           : base(stateManager)
        { }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        private void InitOnEntry()
        {
            var acc1 = CreateMachine(typeof(AccountMachine), new InitializeAccountEvent("A", 100));
            var acc2 = CreateMachine(typeof(AccountMachine), new InitializeAccountEvent("B", 100));

            var amount = this.RandomInteger(150);

            var ev = new InitializeBrokerEvent(null, acc1, acc2, amount);

            this.Monitor<SafetyMonitor>(ev);
            this.Monitor<SafetyMonitor>(new AccountBalanceUpdatedEvent(acc1, 100));
            this.Monitor<SafetyMonitor>(new AccountBalanceUpdatedEvent(acc2, 100));

            var broker = CreateMachine(typeof(BrokerMachine), ev);

            Send(acc2, new EnableEvent());
            Send(acc1, new EnableEvent());

            // TODO: Raising a halt is causing the ClientMachine to stop, and does not allow any of the other created machines to continue.
            //this.Raise(new Halt());
        }

        protected override Task OnActivate()
        {
            return Task.CompletedTask;
        }

    }
}
