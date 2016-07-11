//-----------------------------------------------------------------------
// <copyright file="BangaloreToRedmondTest.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.PSharp.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    [TestClass]
    public class BangaloreToRedmondTest
    {
        class Config : Event
        {
            public MachineId Id;
            public Config(MachineId id) : base(-1, -1) { this.Id = id; }
        }

        class BookCab : Event
        {
            public BookCab() : base(2, -1) { }
        }

        class BookFlight : Event
        {
            public BookFlight() : base(2, -1) { }
        }

        class FlightBooked : Event
        {
            public FlightBooked() : base(2, -1) { }
        }

        class TryAgain : Event
        {
            public TryAgain() : base(2, -1) { }
        }

        class CabBooked : Event
        {
            public CabBooked() : base(2, -1) { }
        }

        class Thanks : Event
        {
            public Thanks() : base(2, -1) { }
        }

        class ReachedAirport : Event
        {
            public ReachedAirport() : base(2, -1) { }
        }

        class MissedFlight : Event
        {
            public MissedFlight() : base(2, -1) { }
        }

        class TookFlight : Event
        {
            public TookFlight() : base(2, -1) { }
        }

        class Unit : Event
        {
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
                if (this.Random())
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
                if (this.Random())
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
                if (this.Random())
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
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Employee));
            }
        }

        [TestMethod]
        public void TestBangaloreToRedmond()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.MaxSchedulingSteps = 300;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute);
            engine.Run();

            //Assert.AreEqual(0, TestingEngine.NumOfFoundBugs);
            //Assert.AreEqual(5, TestingEngine.ExploredDepth);
        }
    }
}
