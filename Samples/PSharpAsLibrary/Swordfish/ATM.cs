using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class ATM : Machine
    {
        internal class Config : Event
        {
            public MachineId Bank;

            public Config(MachineId bank)
                : base()
            {
                this.Bank = bank;
            }
        }

        MachineId Bank;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Bank = (this.ReceivedEvent as Config).Bank;
            this.Goto(typeof(Active));
        }
        
        [OnEventDoAction(typeof(Bank.WithdrawEvent), nameof(DoWithdraw))]
        [OnEventDoAction(typeof(Bank.DepositEvent), nameof(DoDeposit))]
        [OnEventDoAction(typeof(Bank.TransferEvent), nameof(DoTransfer))]
        [OnEventDoAction(typeof(Bank.BalanceInquiryEvent), nameof(DoBalanceInquiry))]
        class Active : MachineState { }
        
        void DoWithdraw()
        {
            var transaction = (this.ReceivedEvent as Bank.WithdrawEvent).Transaction;
            this.Send(this.Bank, new Bank.WithdrawEvent(transaction));
        }

        void DoDeposit()
        {
            var transaction = (this.ReceivedEvent as Bank.DepositEvent).Transaction;
            this.Send(this.Bank, new Bank.DepositEvent(transaction));
        }

        void DoTransfer()
        {
            var transfer = (this.ReceivedEvent as Bank.TransferEvent).Transfer;
            this.Send(this.Bank, new Bank.TransferEvent(transfer));
        }

        void DoBalanceInquiry()
        {
            var transaction = (this.ReceivedEvent as Bank.BalanceInquiryEvent).Transaction;
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(transaction));
        }
    }
}
