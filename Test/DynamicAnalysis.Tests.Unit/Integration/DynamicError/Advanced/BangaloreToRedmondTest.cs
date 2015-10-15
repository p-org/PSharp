//-----------------------------------------------------------------------
// <copyright file="BangaloreToRedmondTest.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.DynamicAnalysis.Tests.Unit
{
    [TestClass]
    public class BangaloreToRedmondTest : BasePSharpTest
    {
        [TestMethod]
        public void TestBangaloreToRedmond()
        {
            var test = @"
using System;
using Microsoft.PSharp;

namespace SystematicTesting
{
    class Config : Event {
        public MachineId Id;
        public Config(MachineId id) : base(-1, -1) { this.Id = id; }
    }

    class BookCab : Event {
        public BookCab() : base(2, -1) { }
    }

    class BookFlight : Event {
        public BookFlight() : base(2, -1) { }
    }

    class FlightBooked : Event {
        public FlightBooked() : base(2, -1) { }
    }

    class TryAgain : Event {
        public TryAgain() : base(2, -1) { }
    }

    class CabBooked : Event {
        public CabBooked() : base(2, -1) { }
    }

    class Thanks : Event {
        public Thanks() : base(2, -1) { }
    }

    class ReachedAirport : Event {
        public ReachedAirport() : base(2, -1) { }
    }

    class MissedFlight : Event {
        public MissedFlight() : base(2, -1) { }
    }

    class TookFlight : Event {
        public TookFlight() : base(2, -1) { }
    }

    class Unit : Event {
        public Unit() : base(2, -1) { }
    }

    class Employee : Machine
    {
        MachineId TravelAgentMachine;
        MachineId CityCabMachine;
        bool Check;
        bool RemoteCheckIn;

        [Start]
        [OnEntry(nameof(EntryInit))]
        [OnEventGotoState(typeof(Unit), typeof(BangaloreOffice))]
        class Init : MachineState { }

        void EntryInit()
        {
            TravelAgentMachine = this.CreateMachine(typeof(TravelAgent));
            this.Send(TravelAgentMachine, new Config(this.Id));
            CityCabMachine = this.CreateMachine(typeof(CityCab));
            this.Send(CityCabMachine, new Config(this.Id));
            RemoteCheckIn = false;
            this.Raise(new Unit());
        }

        [OnEntry(nameof(EntryBangaloreOffice))]
        [OnExit(nameof(ExitBangaloreOffice))]
        [OnEventGotoState(typeof(TryAgain), typeof(BangaloreOffice))]
        [OnEventGotoState(typeof(FlightBooked), typeof(SBookCab))]
        [OnEventPushState(typeof(Unit), typeof(SBookFlight))]
        class BangaloreOffice : MachineState { }

        void EntryBangaloreOffice()
        {
            // push SBookFlight;
            this.Raise(new Unit());
        }

        void ExitBangaloreOffice()
        {
            if (this.ReceivedEvent.GetType() == typeof(FlightBooked))
            {
                this.Send(TravelAgentMachine, new Thanks());
            }
        }

        [OnEntry(nameof(EntrySBookFlight))]
        class SBookFlight : MachineState { }

        void EntrySBookFlight()
        {
            this.Send(TravelAgentMachine, new BookFlight());
            this.Pop();
        }

        [OnEntry(nameof(EntrySBookCab))]
        [OnExit(nameof(ExitSBookCab))]
        [OnEventGotoState(typeof(Default), typeof(TakeBus))]
        [OnEventGotoState(typeof(CabBooked), typeof(TakeCab))]
        class SBookCab : MachineState { }

        void EntrySBookCab()
        {
            this.Send(CityCabMachine, new BookCab());
        }

        void ExitSBookCab()
        {
            this.Assert(RemoteCheckIn == false);
            RemoteCheckIn = true;
            if (this.ReceivedEvent.GetType() != typeof(Default))
            {
                this.Send(CityCabMachine, new Thanks());
            }
        }

        [OnEntry(nameof(EntryTakeCab))]
        [OnEventGotoState(typeof(ReachedAirport), typeof(AtAirport))]
        class TakeCab : MachineState { }

        void EntryTakeCab()
        {
            this.Raise(new ReachedAirport());
        }

        [OnEntry(nameof(EntryTakeBus))]
        [OnEventGotoState(typeof(ReachedAirport), typeof(AtAirport))]
        class TakeBus : MachineState { }

        void EntryTakeBus()
        {
            this.Raise(new ReachedAirport());
        }

        [OnEntry(nameof(EntryAtAirport))]
        [OnExit(nameof(ExitAtAirport))]
        [OnEventGotoState(typeof(TookFlight), typeof(ReachedRedmond))]
        [OnEventGotoState(typeof(MissedFlight), typeof(BangaloreOffice))]
        class AtAirport : MachineState { }

        void EntryAtAirport()
        {
            this.Assert(RemoteCheckIn == true);
            Check = AmILucky();
            if (Check)
            {
                this.Raise(new TookFlight());
            }
            else
            {
                this.Raise(new MissedFlight());
            }
        }

        void ExitAtAirport()
        {
            RemoteCheckIn = false;
        }

        [OnEntry(nameof(EntryReachedRedmond))]
        class ReachedRedmond : MachineState { }

        void EntryReachedRedmond()
        {
            this.Assert(false);
        }

        bool AmILucky()
        {
            if (this.Nondet())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    class TravelAgent : Machine
    {
        MachineId EmployeeMachine;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(Init))]
        class _Init : MachineState { }

        void Configure()
        {
            EmployeeMachine = (this.ReceivedEvent as Config).Id;
            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(BookFlight), typeof(SBookFlight))]
        class Init : MachineState { }

        [OnEntry(nameof(EntrySBookFlight))]
        [OnEventGotoState(typeof(Unit), typeof(Init))]
        [OnEventGotoState(typeof(Thanks), typeof(Init))]
        class SBookFlight : MachineState { }

        void EntrySBookFlight()
        {
            if (this.Nondet())
            {
                this.Send(EmployeeMachine, new TryAgain());
                this.Raise(new Unit());
            }
            else
            {
                this.Send(EmployeeMachine, new FlightBooked());
            }
        }
    }

    class CityCab : Machine
    {
        MachineId EmployeeMachine;

        [Start]
        [OnEventDoAction(typeof(Config), nameof(Configure))]
        [OnEventGotoState(typeof(Unit), typeof(Init))]
        class _Init : MachineState { }

        void Configure()
        {
            EmployeeMachine = (this.ReceivedEvent as Config).Id;
            this.Raise(new Unit());
        }

        [OnEventGotoState(typeof(BookCab), typeof(SBookCab))]
        class Init : MachineState { }

        [OnEntry(nameof(EntrySBookCab))]
        [OnEventGotoState(typeof(Unit), typeof(Init))]
        [OnEventGotoState(typeof(Thanks), typeof(Init))]
        class SBookCab : MachineState { }

        void EntrySBookCab()
        {
            if (this.Nondet())
            {
                this.Raise(new Unit());
            }
            else
            {
                this.Send(EmployeeMachine, new CabBooked());
            }
        }
    }

    public static class TestProgram
    {
        public static void Main(string[] args)
        {
            TestProgram.Execute();
            Console.ReadLine();
        }

        [Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(Employee));
        }
    }
}";

            var parser = new CSharpParser(new PSharpProject(),
                SyntaxFactory.ParseSyntaxTree(test), true);
            var program = parser.Parse();
            program.Rewrite();

            var sctConfig = Configuration.Create();
            sctConfig.SuppressTrace = true;
            sctConfig.Verbose = 2;
            sctConfig.SchedulingStrategy = SchedulingStrategy.DFS;

            var assembly = base.GetAssembly(program.GetSyntaxTree());
            var context = AnalysisContext.Create(sctConfig, assembly);
            var sctEngine = SCTEngine.Create(context).Run();

            //Assert.AreEqual(0, SCTEngine.NumOfFoundBugs);
            //Assert.AreEqual(5, SCTEngine.ExploredDepth);
        }
    }
}
