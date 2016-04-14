using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class CheckingAccount : Account
    {
        Double Rate = 0.0;

        [Start]
        class Init : MachineState { }

        void OnEntry()
        {
            base.Cents = 0;
            base.IsLocked = false;
            base.Counter = 0;
            base.TransferTransaction = null;

            base.Pin = (this.ReceivedEvent as Account.Config).Pin;
            base.Cents = (this.ReceivedEvent as Account.Config).Ammount;

            this.Goto(typeof(Active));
        }

        protected override void DoUpdate() { }
    }
}
