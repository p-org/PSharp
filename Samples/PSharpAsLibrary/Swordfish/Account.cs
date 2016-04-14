using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal abstract class Account : Machine
    {
        internal class Config : Event
        {
            public String Pin;
            public Integer Ammount;
            public Double Rate;

            public Config(String pin, Integer ammount)
                : base()
            {
                this.Pin = pin;
                this.Ammount = ammount;
            }

            public Config(String pin, Integer ammount, Double rate)
                : base()
            {
                this.Pin = pin;
                this.Ammount = ammount;
                this.Rate = rate;
            }
        }

        internal class Close : Event
        {
            public MachineId Bank;

            public Close(MachineId bank)
                : base()
            {
                this.Bank = bank;
            }
        }

        internal class CloseAck : Event
        {
            public Boolean Result;

            public CloseAck(Boolean result)
                : base()
            {
                this.Result = result;
            }
        }

        internal class WithdrawEvent : Event
        {
            public Transaction Transaction;
            public Boolean Multiples;

            public WithdrawEvent(Transaction transaction, Boolean multiples)
                : base()
            {
                this.Transaction = transaction;
                this.Multiples = multiples;
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
            public MachineId Account;
            public Transfer Transfer;

            public TransferEvent(MachineId account, Transfer transfer)
                : base()
            {
                this.Account = account;
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

        internal class TransferComplete : Event
        {
            public Response Response;

            public TransferComplete(Response response)
                : base()
            {
                this.Response = response;
            }
        }

        internal class Update : Event { }

        protected String Pin;
        protected Integer Cents;
        protected Boolean IsLocked;
        protected Integer Counter;
        protected Transfer TransferTransaction;
        
        [OnEventDoAction(typeof(Account.WithdrawEvent), nameof(DoWithdraw))]
        [OnEventDoAction(typeof(Account.DepositEvent), nameof(DoDeposit))]
        [OnEventDoAction(typeof(Account.BalanceInquiryEvent), nameof(DoBalanceInquiry))]
        [OnEventDoAction(typeof(Account.LockEvent), nameof(DoLock))]
        [OnEventDoAction(typeof(Account.UnlockEvent), nameof(DoUnlock))]
        [OnEventDoAction(typeof(Account.Close), nameof(DoClose))]
        [OnEventDoAction(typeof(Account.TransferEvent), nameof(DoTransfer))]
        [OnEventDoAction(typeof(Account.TransferComplete), nameof(DoTransferComplete))]
        [OnEventDoAction(typeof(Account.Update), nameof(DoUpdate))]
        protected class Active : MachineState { }

        void DoWithdraw()
        {
            var transaction = (this.ReceivedEvent as Account.WithdrawEvent).Transaction;
            var multiples = (this.ReceivedEvent as Account.WithdrawEvent).Multiples;

            if (this.IsLocked)
            {
                this.Send(transaction, new FailureResponse("Account is locked."));
                return;
            }

            if (!this.CheckPin(transaction.GetPin()))
            {
                this.Send(transaction, new FailureResponse("Invalid PIN."));
                return;
            }

            if (!multiples.BooleanValue() && (transaction.GetAmount() % 2000 != 0))
            {
                this.Send(transaction, new FailureResponse("Withdrawals must be in multiples of 20."));
                return;
            }

            if ((transaction.GetAmount() < 0) || (transaction.GetAmount() > this.Cents))
            {
                this.Send(transaction, new FailureResponse("Withdraw failed."));
            }
            else
            {
                this.Cents = this.Cents - transaction.GetAmount();
                this.Send(transaction, new SuccessResponse("Withdraw succeeded."));
            }
        }

        void DoDeposit()
        {
            var transaction = (this.ReceivedEvent as Account.DepositEvent).Transaction;

            if (this.IsLocked)
            {
                this.Send(transaction, new FailureResponse("Account is locked."));
                return;
            }

            if (!this.CheckPin(transaction.GetPin()))
            {
                this.Send(transaction, new FailureResponse("Invalid PIN."));
                return;
            }

            Double amount = new Double(transaction.GetAmount());

            if (amount < 0)
            {
                this.Send(transaction, new FailureResponse("Deposit failed."));
            }
            else
            {
                this.Cents = this.Cents + (int)amount;
                this.Send(transaction, new SuccessResponse("Deposit succeeded."));
            }
        }

        void DoBalanceInquiry()
        {
            var transaction = (this.ReceivedEvent as Account.BalanceInquiryEvent).Transaction;

            if (this.IsLocked)
            {
                this.Send(transaction, new FailureResponse("Account is locked."));
                return;
            }

            if (this.TransferTransaction != null)
            {
                this.Send(transaction, new FailureResponse("Transaction cannot be completed at this time."));
                return;
            }

            if (!this.CheckPin(transaction.GetPin()))
            {
                this.Send(transaction, new FailureResponse("Invalid PIN."));
                return;
            }

            this.Send(transaction, new BalanceResponse(this.Cents));
        }

        void DoLock()
        {
            var transaction = (this.ReceivedEvent as Account.LockEvent).Transaction;
            this.IsLocked = true;
            this.Send(transaction, new SuccessResponse("Account successfully locked!"));
        }

        void DoUnlock()
        {
            var transaction = (this.ReceivedEvent as Account.UnlockEvent).Transaction;
            this.Counter = 0;
            this.IsLocked = false;
            this.Send(transaction, new SuccessResponse("Account successfully unlocked!"));
        }

        void DoClose()
        {
            var bank = (this.ReceivedEvent as Account.Close).Bank;
            this.Send(bank, new CloseAck(this.Cents == 0 && this.TransferTransaction == null));
            this.Raise(new Halt());
        }

        void DoTransfer()
        {
            var fromAccount = (this.ReceivedEvent as Account.TransferEvent).Account;
            var transfer = (this.ReceivedEvent as Account.TransferEvent).Transfer;

            if (this.IsLocked)
            {
                this.Send(transfer, new FailureResponse("Destination account is locked."));
                return;
            }

            if (this.TransferTransaction != null)
            {
                this.Send(transfer, new FailureResponse("Transaction cannot be completed at this time."));
                return;
            }

            this.TransferTransaction = transfer;
            var wTrans = new Transaction(this.Id, typeof(TransferComplete), new Integer(0),
                transfer.GetAmount(), transfer.GetPin(), 0, 0.0);

            this.Send(fromAccount, new Account.WithdrawEvent(wTrans, new Boolean(false)));
        }

        void DoTransferComplete()
        {
            var response = (this.ReceivedEvent as Account.TransferComplete).Response;

            if (response is SuccessResponse)
            {
                this.Cents = this.Cents + this.TransferTransaction.GetAmount();
                this.Send(this.TransferTransaction, new SuccessResponse("Transfer succeeded!"));
            }
            else
            {
                this.Send(this.TransferTransaction, new FailureResponse("Withdraw during transfer failed."));
            }

            this.TransferTransaction = null;
        }

        protected abstract void DoUpdate();

        private Boolean CheckPin(String p)
        {
            if (!this.IsLocked && p.StringValue().Trim().Equals(this.Pin))
            {
                this.Counter = 0;
                return true;
            }
            else
            {
                this.Counter = this.Counter + 1;

                if (this.Counter >= 3)
                {
                    this.IsLocked = true;
                }

                return false;
            }
        }

        private void Send(Transaction trans, Response resp)
        {
            this.Send(trans.GetMachine(), Activator.CreateInstance(trans.GetCallback(), resp) as Event);
        }
    }
}
