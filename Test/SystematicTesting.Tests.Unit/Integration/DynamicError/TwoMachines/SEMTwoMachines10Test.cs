//-----------------------------------------------------------------------
// <copyright file="SEMTwoMachines10Test.cs" company="Microsoft">
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

using Microsoft.PSharp.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.SystematicTesting.Tests.Unit
{
    [TestClass]
    public class SEMTwoMachines10Test : BasePSharpTest
    {
        class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        class E2 : Event
        {
            public bool Value;
            public E2(bool value) : base(1, -1) { this.Value = value; }
        }

        class Real1 : Machine
        {
            bool test = false;
            MachineId mac;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(Default), typeof(S1))]
            [OnEventDoAction(typeof(E1), nameof(Action1))]
            class Init : MachineState { }

            void EntryInit()
            {
                mac = this.CreateMachine(typeof(Real2));
                this.Raise(new E1());
            }

            void ExitInit()
            {
                this.Send(mac, new E2(test));
            }

            class S1 : MachineState { }

            void Action1()
            {
                test = true;
            }
        }

        class Real2 : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventDoAction(typeof(E2), nameof(EntryAction))]
            class Init : MachineState { }

            void EntryInit() { }

            void EntryAction()
            {
                if (this.ReceivedEvent.GetType() == typeof(E2))
                {
                    Action2();
                }
                else
                {
                    //this.Assert(false); // unreachable
                }
            }

            void Action2()
            {
                this.Assert((this.ReceivedEvent as E2).Value == false); // reachable
            }
        }

        class M : Monitor
        {
            [Start]
            [OnEntry(nameof(EntryX))]
            class X : MonitorState { }

            void EntryX()
            {
                //this.Assert((this.ReceivedEvent as E2).Value == true); // reachable
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Real1));
            }
        }

        [TestMethod]
        public void TestTwoMachines10()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(1, engine.NumOfFoundBugs);
        }
    }
}
