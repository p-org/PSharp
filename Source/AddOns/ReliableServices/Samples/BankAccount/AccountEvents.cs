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

    class InitializeAccountEvent : RsmInitEvent
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
        public IRsmId Mid;
        public int Balance;

        public AccountBalanceUpdatedEvent(IRsmId Mid, int balance)
        {
            this.Mid = Mid;
            this.Balance = balance;
        }
    }

    class InitializeBrokerEvent : RsmInitEvent
    {
        public IRsmId Client;
        public IRsmId Source;
        public IRsmId Target;
        public int Amount;

        public InitializeBrokerEvent(IRsmId Client, IRsmId source, IRsmId target, int amount)
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
        public IRsmId sender;
        public int amount;

        public DepositEvent(IRsmId sender, int amount)
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
        public IRsmId sender;
        public int amount;

        public WithdrawEvent(IRsmId sender, int amount)
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
