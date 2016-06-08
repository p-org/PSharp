using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Swordfish
{
    internal class Driver : Machine
    {
        internal class CreateCallback : Event
        {
            public Response Response;

            public CreateCallback(Response response)
                : base()
            {
                this.Response = response;
            }
        }

        MachineId Bank;
        MachineId ATM;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Bank = this.CreateMachine(typeof(Bank), new Bank.Config(this.Id));
            this.ATM = this.CreateMachine(typeof(ATM), new ATM.Config(this.Bank));

            this.Goto(typeof(Test1));
        }

        [OnEntry(nameof(Test1OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test2))]
        class Test1 : MachineState { }

        void Test1OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(10000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test2OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test3))]
        class Test2 : MachineState { }

        void Test2OnEntry()
        {
            this.Send(this.Bank, new Bank.CloseAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(1), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test3OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test4))]
        class Test3 : MachineState { }

        void Test3OnEntry()
        {
            this.Send(this.Bank, new Bank.CloseAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(2), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test4OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test5))]
        class Test4 : MachineState { }

        void Test4OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(0),
                new String("1234"), new Integer(0), new Double(1.0))));
        }

        [OnEntry(nameof(Test5OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test6))]
        class Test5 : MachineState { }

        void Test5OnEntry()
        {
            this.Send(this.Bank, new Bank.CloseAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(2), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test6OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test7))]
        class Test6 : MachineState { }

        void Test6OnEntry()
        {
            this.Send(this.Bank, new Bank.CloseAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(2), new Integer(0), new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test7OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test8))]
        class Test7 : MachineState { }

        void Test7OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test8OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test9))]
        class Test8 : MachineState { }

        void Test8OnEntry()
        {
            this.Send(this.Bank, new Bank.DepositEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(3), new Integer(2500),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test9OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test10))]
        class Test9 : MachineState { }

        void Test9OnEntry()
        {
            this.Send(this.Bank, new Bank.DepositEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(15), new Integer(1000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test10OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test11))]
        class Test10 : MachineState { }

        void Test10OnEntry()
        {
            this.Send(this.Bank, new Bank.WithdrawEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(3), new Integer(2000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test11OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test12))]
        class Test11 : MachineState { }

        void Test11OnEntry()
        {
            this.Send(this.Bank, new Bank.WithdrawEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(15), new Integer(500),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test12OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test13))]
        class Test12 : MachineState { }

        void Test12OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(4000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test13OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test14))]
        class Test13 : MachineState { }

        void Test13OnEntry()
        {
            this.Send(this.Bank, new Bank.TransferEvent(new Transfer(
                this.Id, typeof(CreateCallback), new Integer(4), new Integer(3),
                new Integer(2000), new String("1234"))));
        }

        [OnEntry(nameof(Test14OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test15))]
        class Test14 : MachineState { }

        void Test14OnEntry()
        {
            this.Send(this.Bank, new Bank.TransferEvent(new Transfer(
                this.Id, typeof(CreateCallback), new Integer(20), new Integer(3),
                new Integer(200), new String("1234"))));
        }

        [OnEntry(nameof(Test15OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test16))]
        class Test15 : MachineState { }

        void Test15OnEntry()
        {
            this.Send(this.Bank, new Bank.TransferEvent(new Transfer(
                this.Id, typeof(CreateCallback), new Integer(4),
                new Integer(30), new Integer(200), new String("1234"))));
        }

        [OnEntry(nameof(Test16OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test17))]
        class Test16 : MachineState { }

        void Test16OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(2500),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test17OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test18))]
        class Test17 : MachineState { }

        void Test17OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(5), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test18OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test19))]
        class Test18 : MachineState { }

        void Test18OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(3), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test19OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test20))]
        class Test19 : MachineState { }

        void Test19OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(10), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test20OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test21))]
        class Test20 : MachineState { }

        void Test20OnEntry()
        {
            this.Send(this.Bank, new Bank.WithdrawEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(5), 6000, new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test21OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test22))]
        class Test21 : MachineState { }

        void Test21OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(0), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test22OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test23))]
        class Test22 : MachineState { }

        void Test22OnEntry()
        {
            this.Send(this.Bank, new Bank.DepositEvent(new Transaction(
                this.Id, typeof(CreateCallback), 6, new Integer(2500), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test23OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test24))]
        class Test23 : MachineState { }

        void Test23OnEntry()
        {
            this.Send(this.Bank, new Bank.DepositEvent(new Transaction(
                this.Id, typeof(CreateCallback), 6, new Integer(1000), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test24OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test25))]
        class Test24 : MachineState { }

        void Test24OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), 6, new Integer(0), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test25OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test26))]
        class Test25 : MachineState { }

        void Test25OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test26OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test27))]
        class Test26 : MachineState { }

        void Test26OnEntry()
        {
            this.Send(this.Bank, new Bank.DepositEvent(new Transaction(
                this.Id, typeof(CreateCallback), 7, 3000, new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test27OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test28))]
        class Test27 : MachineState { }

        void Test27OnEntry()
        {
            this.Send(this.Bank, new Bank.WithdrawEvent(new Transaction(
                this.Id, typeof(CreateCallback), 7, 1500, new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test28OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test29))]
        class Test28 : MachineState { }

        void Test28OnEntry()
        {
            this.Send(this.Bank, new Bank.WithdrawEvent(new Transaction(
                this.Id, typeof(CreateCallback), 7, new Integer(2000), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test29OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test30))]
        class Test29 : MachineState { }

        void Test29OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(4000), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test30OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test31))]
        class Test30 : MachineState { }

        void Test30OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(4000), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test31OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test32))]
        class Test31 : MachineState { }

        void Test31OnEntry()
        {
            this.Send(this.Bank, new Bank.TransferEvent(new Transfer(
                this.Id, typeof(CreateCallback), 8, 9, 6000, new String("1234"))));
        }

        [OnEntry(nameof(Test32OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test33))]
        class Test32 : MachineState { }

        void Test32OnEntry()
        {
            this.Send(this.Bank, new Bank.TransferEvent(new Transfer(
                this.Id, typeof(CreateCallback), 8, 9, new Integer(2000), new String("1234"))));
        }

        [OnEntry(nameof(Test33OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test34))]
        class Test33 : MachineState { }

        void Test33OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), 8, new Integer(0), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test34OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test35))]
        class Test34 : MachineState { }

        void Test34OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), 9, new Integer(0), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test35OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test36))]
        class Test35 : MachineState { }

        void Test35OnEntry()
        {
            this.Send(this.Bank, new Bank.LockEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(12), new Integer(10000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test36OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test37))]
        class Test36 : MachineState { }

        void Test36OnEntry()
        {
            this.Send(this.Bank, new Bank.UnlockEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(12), new Integer(10000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test37OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test38))]
        class Test37 : MachineState { }

        void Test37OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(10000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test38OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test39))]
        class Test38 : MachineState { }

        void Test38OnEntry()
        {
            this.Send(this.Bank, new Bank.LockEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(10), new Integer(10000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test39OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test40))]
        class Test39 : MachineState { }

        void Test39OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(10), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test40OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test41))]
        class Test40 : MachineState { }

        void Test40OnEntry()
        {
            this.Send(this.Bank, new Bank.UnlockEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(12), new Integer(10000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test41OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test42))]
        class Test41 : MachineState { }

        void Test41OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(10), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test42OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test43))]
        class Test42 : MachineState { }

        void Test42OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(10000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test43OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test44))]
        class Test43 : MachineState { }

        void Test43OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(11), new Integer(0),
                new String("1235"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test44OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test45))]
        class Test44 : MachineState { }

        void Test44OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(11), new Integer(0),
                new String("1235"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test45OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test46))]
        class Test45 : MachineState { }

        void Test45OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(11), new Integer(0),
                new String("1235"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test46OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test47))]
        class Test46 : MachineState { }

        void Test46OnEntry()
        {
            this.Send(this.Bank, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(11), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test47OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test48))]
        class Test47 : MachineState { }

        void Test47OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test48OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test49))]
        class Test48 : MachineState { }

        void Test48OnEntry()
        {
            this.Send(this.ATM, new Bank.DepositEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(12), new Integer(2500),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test49OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test50))]
        class Test49 : MachineState { }

        void Test49OnEntry()
        {
            this.Send(this.ATM, new Bank.DepositEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(15), new Integer(1000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test50OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test51))]
        class Test50 : MachineState { }

        void Test50OnEntry()
        {
            this.Send(this.ATM, new Bank.WithdrawEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(12), new Integer(2000),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test51OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test52))]
        class Test51 : MachineState { }

        void Test51OnEntry()
        {
            this.Send(this.ATM, new Bank.WithdrawEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(13), new Integer(500),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test52OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test53))]
        class Test52 : MachineState { }

        void Test52OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(4000), new String("1234"),
                new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test53OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test54))]
        class Test53 : MachineState { }

        void Test53OnEntry()
        {
            this.Send(this.ATM, new Bank.TransferEvent(new Transfer(
                this.Id, typeof(CreateCallback), new Integer(13), new Integer(12),
                new Integer(2000), new String("1234"))));
        }

        [OnEntry(nameof(Test54OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test55))]
        class Test54 : MachineState { }

        void Test54OnEntry()
        {
            this.Send(this.ATM, new Bank.TransferEvent(new Transfer(
                this.Id, typeof(CreateCallback), new Integer(20), new Integer(12),
                new Integer(200), new String("1234"))));
        }

        [OnEntry(nameof(Test55OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test56))]
        class Test55 : MachineState { }

        void Test55OnEntry()
        {
            this.Send(this.ATM, new Bank.TransferEvent(new Transfer(
                this.Id, typeof(CreateCallback), new Integer(13), new Integer(30),
                new Integer(200), new String("1234"))));
        }

        [OnEntry(nameof(Test56OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test57))]
        class Test56 : MachineState { }

        void Test56OnEntry()
        {
            this.Send(this.Bank, new Bank.CreateAccountEvent(new Transaction(
                this.Id, typeof(CreateCallback), null, new Integer(2500),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test57OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test58))]
        class Test57 : MachineState { }

        void Test57OnEntry()
        {
            this.Send(this.ATM, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(14), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test58OnEntry))]
        [OnEventGotoState(typeof(CreateCallback), typeof(Test59))]
        class Test58 : MachineState { }

        void Test58OnEntry()
        {
            this.Send(this.ATM, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(12), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }

        [OnEntry(nameof(Test59OnEntry))]
        [IgnoreEvents(typeof(CreateCallback))]
        class Test59 : MachineState { }

        void Test59OnEntry()
        {
            this.Send(this.ATM, new Bank.BalanceInquiryEvent(new Transaction(
                this.Id, typeof(CreateCallback), new Integer(20), new Integer(0),
                new String("1234"), new Integer(1), new Double(1.0))));
        }
    }
}
