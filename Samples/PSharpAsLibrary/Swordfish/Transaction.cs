using System;
using Microsoft.PSharp; 

namespace Swordfish
{
    internal class Transfer : Transaction
    {
        private Integer From;

        public Transfer(MachineId machine, Type callback, Integer from, Integer to,
            Integer amount, string pin)
            : base(machine, callback, to, amount, pin, 3, 0)
        {
            this.From = from;
        }

        public Integer GetFrom()
        {
            return this.From;
        }
    }

    internal class Transaction
    {
        private MachineId Machine;

        private Type Callback;

        private Integer AccountNumber;

        private Integer Amount;

        private String Pin;

        private Integer Type;

        private Double Rate;

        public Transaction(MachineId machine, Type callback, Integer accountNumber, Integer amount,
            String pin, Integer type, Double rate)
        {
            this.Machine = machine;
            this.Callback = callback;
            this.AccountNumber = accountNumber;
            this.Amount = amount;
            this.Pin = pin;

            if (type == 3)
            {
                this.Type = 3;
                this.Rate = new Double(0.0);
                return;
            }

            if ((type != 0) && (type != 1))
            {
                throw new Exception("Invalid account type.");
            }

            this.Type = type;
            this.Rate = rate;
        }

        public MachineId GetMachine()
        {
            return this.Machine;
        }

        public Type GetCallback()
        {
            return this.Callback;
        }

        public Integer GetAccountNumber()
        {
            return this.AccountNumber;
        }

        public Integer GetAmount()
        {
            return this.Amount;
        }

        public String GetPin()
        {
            return this.Pin;
        }

        public Integer GetAccountType()
        {
            return this.Type;
        }

        public Double GetRate()
        {
            return this.Rate;
        }
    }
}
