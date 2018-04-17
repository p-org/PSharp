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
    class ClientMachine : Machine
    {
        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        async Task InitOnEntry()
        {
            var origHost = (this.ReceivedEvent as InitClientEvent).Host;

            var acc1 = await origHost.ReliableCreateMachine<AccountMachine>(new InitializeAccountEvent("A", 100));
            var acc2 = await origHost.ReliableCreateMachine<AccountMachine>(new InitializeAccountEvent("B", 100));

            var amount = this.RandomInteger(150);

            var ev = new InitializeBrokerEvent(null, acc1, acc2, amount);

            this.Monitor<SafetyMonitor>(ev);
            this.Monitor<SafetyMonitor>(new AccountBalanceUpdatedEvent(acc1, 100));
            this.Monitor<SafetyMonitor>(new AccountBalanceUpdatedEvent(acc2, 100));

            var broker = await origHost.ReliableCreateMachine<BrokerMachine>(ev);

            await origHost.ReliableSend(acc2, new EnableEvent());
            await origHost.ReliableSend(acc1, new EnableEvent());

            this.Raise(new Halt());
        }

    }
}
