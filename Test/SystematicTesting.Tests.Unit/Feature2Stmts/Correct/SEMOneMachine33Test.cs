//-----------------------------------------------------------------------
// <copyright file="SEMOneMachine33Test.cs">
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
using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.SystematicTesting.Tests.Unit
{
    [TestClass]
    public class SEMOneMachine33Test
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
