using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;
using Microsoft.PSharp.ReliableServices;
using Microsoft.PSharp.ReliableServices.Utilities;

namespace BankAccount
{

    class InitializeAccountEvent : Event
    {
        public string Name;
        public int Balance;

        public InitializeAccountEvent(string name, int balance)
        {
            this.Name = name;
            this.Balance = balance;
        }
    }

    class AccountBalanceUpdatedEvent : Event
    {
        public MachineId Mid;
        public int Balance;

        public AccountBalanceUpdatedEvent(MachineId Mid, int balance)
        {
            this.Mid = Mid;
            this.Balance = balance;
        }
    }

    class InitializeBrokerEvent : Event
    {
        public MachineId Client;
        public MachineId Source;
        public MachineId Target;
        public int Amount;

        public InitializeBrokerEvent(MachineId Client, MachineId source, MachineId target, int amount)
        {
            this.Client = Client;
            this.Source = source;
            this.Target = target;
            this.Amount = amount;
        }
    }

    /// <summary>
    /// Deposit operation
    /// </summary>
    class DepositEvent : Event
    {
        public MachineId sender;
        public int amount;

        public DepositEvent(MachineId sender, int amount)
        {
            this.sender = sender;
            this.amount = amount;
        }
    }

    /// <summary>
    /// Withdraw operation
    /// </summary>
    class WithdrawEvent : Event
    {
        public MachineId sender;
        public int amount;

        public WithdrawEvent(MachineId sender, int amount)
        {
            this.sender = sender;
            this.amount = amount;
        }
    }

    class SuccessEvent : Event { }

    class FailureEvent : Event { }

    class AbortedEvent : Event { }

    class EnableEvent : Event { }
}
