using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class Bank : Machine
    {
        internal class Config : Event
        {
            public MachineId Driver;

            public Config(MachineId driver)
                : base()
            {
                this.Driver = driver;
            }
        }

        internal class CreateAccountEvent : Event
        {
            public Transaction Transaction;

            public CreateAccountEvent(Transaction transaction)
                : base()
            {
                this.Transaction = transaction;
            }
        }

        internal class CloseAccountEvent : Event
        {
            public Transaction Transaction;

            public CloseAccountEvent(Transaction transaction)
                : base()
            {
                this.Transaction = transaction;
            }
        }

        internal class WithdrawEvent : Event
        {
            public Transaction Transaction;

            public WithdrawEvent(Transaction transaction)
                : base()
            {
                this.Transaction = transaction;
            }
        }

        internal class DepositEvent : Event
        {
            public Transaction Transaction;

            public DepositEvent(Transaction transaction)
                : base()
            {
                this.Transaction = transaction;
            }
        }

        internal class TransferEvent : Event
        {
            public Transfer Transfer;

            public TransferEvent(Transfer transfer)
                : base()
            {
                this.Transfer = transfer;
            }
        }

        internal class BalanceInquiryEvent : Event
        {
            public Transaction Transaction;

            public BalanceInquiryEvent(Transaction transaction)
                : base()
            {
                this.Transaction = transaction;
            }
        }

        internal class LockEvent : Event
        {
            public Transaction Transaction;

            public LockEvent(Transaction transaction)
                : base()
            {
                this.Transaction = transaction;
            }
        }

        internal class UnlockEvent : Event
        {
            public Transaction Transaction;

            public UnlockEvent(Transaction transaction)
                : base()
            {
                this.Transaction = transaction;
            }
        }
        
        MachineId Driver;

        Dictionary<Integer, MachineId> Accounts;

        Integer AccountIds = 0;

        Transaction TransBeingProcessed;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Driver = (this.ReceivedEvent as Config).Driver;
            this.Accounts = new Dictionary<Integer, MachineId>();
            this.TransBeingProcessed = null;

            this.Goto(typeof(Active));
        }
        
        [OnEventDoAction(typeof(CreateAccountEvent), nameof(DoCreateAccount))]
        [OnEventDoAction(typeof(CloseAccountEvent), nameof(DoCloseAccount))]
        [OnEventDoAction(typeof(WithdrawEvent), nameof(DoWithdraw))]
        [OnEventDoAction(typeof(DepositEvent), nameof(DoDeposit))]
        [OnEventDoAction(typeof(TransferEvent), nameof(DoTransfer))]
        [OnEventDoAction(typeof(BalanceInquiryEvent), nameof(DoBalanceInquiry))]
        [OnEventDoAction(typeof(LockEvent), nameof(DoLockAccount))]
        [OnEventDoAction(typeof(UnlockEvent), nameof(DoUnlockAccount))]
        class Active : MachineState { }

        [OnEventDoAction(typeof(Account.CloseAck), nameof(DoCloseAccountAck))]
        class WaitingAccountToClose : MachineState { }

        void DoCreateAccount()
        {
            var transaction = (this.ReceivedEvent as CreateAccountEvent).Transaction;

            MachineId newAccount = null;

            Integer accountNumber = this.AccountIds + 1;

            if (transaction.GetAccountType() == 0)
            {
                newAccount = this.CreateMachine(typeof(CheckingAccount), new CheckingAccount.Config(
                    transaction.GetPin(), transaction.GetAmount()));
            }
            else if (transaction.GetAccountType() == 1)
            {
                newAccount = this.CreateMachine(typeof(SavingsAccount), new SavingsAccount.Config(
                    transaction.GetPin(), transaction.GetAmount(), transaction.GetRate()));
            }
            else
            {
                this.Send(transaction, new FailureResponse("Illegal account type."));
                return;
            }

            this.Accounts.Add(accountNumber, newAccount);
            this.AccountIds = this.AccountIds + 1;

            this.Send(transaction, new OpenedResponse(accountNumber));
        }

        void DoCloseAccount()
        {
            var transaction = (this.ReceivedEvent as CloseAccountEvent).Transaction;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account."));
                return;
            }

            var acccount = this.Accounts[transaction.GetAccountNumber()];

            this.Send(acccount, new Account.Close(this.Id));
            this.TransBeingProcessed = transaction;

            this.Goto(typeof(WaitingAccountToClose));
        }

        void DoCloseAccountAck()
        {
            var result = (this.ReceivedEvent as Account.CloseAck).Result;

            if (result.BooleanValue())
            {
                this.Accounts.Remove(this.TransBeingProcessed.GetAccountNumber());
                this.Send(this.TransBeingProcessed, new SuccessResponse("Account closed."));
                this.TransBeingProcessed = null;
            }
            else
            {
                this.Send(this.TransBeingProcessed, new FailureResponse("Account not closed: nonzero balance."));
                this.TransBeingProcessed = null;
            }

            this.Goto(typeof(Active));
        }

        void DoWithdraw()
        {
            var transaction = (this.ReceivedEvent as WithdrawEvent).Transaction;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account."));
                return;
            }

            var account = this.Accounts[transaction.GetAccountNumber()];
            this.Send(account, new Account.WithdrawEvent(transaction, new Boolean(true)));
        }

        void DoDeposit()
        {
            var transaction = (this.ReceivedEvent as DepositEvent).Transaction;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account."));
                return;
            }

            var account = this.Accounts[transaction.GetAccountNumber()];
            this.Send(account, new Account.DepositEvent(transaction));
        }

        void DoTransfer()
        {
            var transfer = (this.ReceivedEvent as TransferEvent).Transfer;

            if (!this.Accounts.ContainsKey(transfer.GetFrom()))
            {
                this.Send(transfer, new FailureResponse("No such account: " + transfer.GetFrom()));
                return;
            }

            var fromAccount = this.Accounts[transfer.GetFrom()];

            if (!this.Accounts.ContainsKey(transfer.GetAccountNumber()))
            {
                this.Send(transfer, new FailureResponse("No such account: " + transfer.GetAccountNumber()));
                return;
            }

            var toAccount = this.Accounts[transfer.GetAccountNumber()];
            this.Send(toAccount, new Account.TransferEvent(fromAccount, transfer));
        }

        void DoBalanceInquiry()
        {
            var transaction = (this.ReceivedEvent as BalanceInquiryEvent).Transaction;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account: " + transaction.GetAccountNumber()));
                return;
            }

            var account = this.Accounts[transaction.GetAccountNumber()];

            this.Send(account, new Account.BalanceInquiryEvent(transaction));
        }

        void DoLockAccount()
        {
            Console.WriteLine("[Bank] is Locking Account ...\n");

            var transaction = (this.ReceivedEvent as LockEvent).Transaction;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account: " + transaction.GetAccountNumber()));
                return;
            }

            var account = this.Accounts[transaction.GetAccountNumber()];

            this.Send(account, new Account.LockEvent(transaction));
        }

        void DoUnlockAccount()
        {
            var transaction = (this.ReceivedEvent as UnlockEvent).Transaction;

            if (!this.Accounts.ContainsKey(transaction.GetAccountNumber()))
            {
                this.Send(transaction, new FailureResponse("No such account: " + transaction.GetAccountNumber()));
                return;
            }

            var account = this.Accounts[transaction.GetAccountNumber()];

            this.Send(account, new Account.UnlockEvent(transaction));
        }

        void Send(Transaction transaction, Response response)
        {
            this.Send(transaction.GetMachine(), Activator.CreateInstance(transaction.GetCallback(), response) as Event);
        }
    }
}
