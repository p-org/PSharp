//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine33Test.cs" company="Microsoft">
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
using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.SystematicTesting.Tests.Unit
{
    [TestClass]
    public class SEMOneMachine33Test : BasePSharpTest
    {
        class Config : Event
        {
            public List<int> List;
            public int V;
            public Config(List<int> l, int v) : base(-1, -1) { this.List = l; this.V = v; }
        }

        class Unit : Event { }

        class SeqPayload : Event
        {
            public List<int> List;
            public SeqPayload(List<int> l) : base(-1, -1) { this.List = l; }
        }

        class Entry : Machine
        {
            List<int> l;
            MachineId mac;

            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                l = new List<int>();
                l.Insert(0, 12);
                l.Insert(0, 23);
                l.Insert(0, 12);
                l.Insert(0, 23);
                l.Insert(0, 12);
                l.Insert(0, 23);
                mac = this.CreateMachine(typeof(Tester));
                this.Send(mac, new Config(l, 1));
                this.Send(mac, new SeqPayload(l));
            }
        }

        class Tester : Machine
        {
            List<int> ii;
            List<int> rec;
            int i;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(SeqPayload), typeof(TestItNow))]
            class Init : MachineState { }

            void EntryInit()
            {
                ii = new List<int>();
                rec = new List<int>();
            }

            void Configure()
            {
                ii = (this.ReceivedEvent as Config).List;
                this.Assert(ii[0] == 23);
                this.Assert((this.ReceivedEvent as Config).V == 1);
            }

            [OnEntry(nameof(EntryTestItNow))]
            class TestItNow : MachineState { }

            void EntryTestItNow()
            {
                rec = (this.ReceivedEvent as SeqPayload).List;
                i = rec.Count - 1;
                while (i >= 0)
                {
                    this.Assert(rec[i] == ii[i]);
                    i = i - 1;
                }
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Entry));
            }
        }

        [TestMethod]
        public void TestSEMOneMachine33()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            configuration.SchedulingIterations = 5;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(0, engine.NumOfFoundBugs);
        }
    }
}
