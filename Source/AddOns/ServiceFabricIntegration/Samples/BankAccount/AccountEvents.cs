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

    class InitializeBrokerEvent : Event
    {
        public MachineId Source;
        public MachineId Target;
        public int Amount;

        public InitializeBrokerEvent(MachineId source, MachineId target, int amount)
        {
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


    class EnableEvent : Event { }
}
