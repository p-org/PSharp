//-----------------------------------------------------------------------
// <copyright file="UnfairExecutionTest.cs">
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
using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    [TestClass]
    public class UnfairExecutionTest
    {
        class Unit : Event { }

        class E : Event
        {
            public MachineId A;

            public E(MachineId a)
            {
                this.A = a;
            }
        }

        class M : Machine
        {
            MachineId N;

            [Start]
            [OnEntry(nameof(SOnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            class S : MachineState { }

            void SOnEntry()
            {
                this.N = this.CreateMachine(typeof(N));
                this.Send(this.N, new E(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(S2OnEntry))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            [OnEventGotoState(typeof(E), typeof(S3))]
            class S2 : MachineState { }

            void S2OnEntry()
            {
                this.Send(this.Id, new Unit());
            }

            [OnEntry(nameof(S3OnEntry))]
            class S3 : MachineState { }

            void S3OnEntry()
            {
                this.Monitor<LivenessMonitor>(new E(this.Id));
                this.Raise(new Halt());
            }
        }

        class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Unit), nameof(Foo))]
            class S : MachineState { }

            void Foo()
            {
                this.Send((this.ReceivedEvent as E).A, new E(this.Id));
            }
        }

        class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E), typeof(S2))]
            class S : MonitorState { }

            [Cold]
            class S2 : MonitorState { }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.RegisterMonitor(typeof(LivenessMonitor));
                runtime.CreateMachine(typeof(M));
            }
        }

        [TestMethod]
        public void TestUnfairExecution()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 3;
            configuration.LivenessTemperatureThreshold = 150;
            configuration.SchedulingStrategy = SchedulingStrategy.PCT;
            configuration.MaxSchedulingSteps = 300;

            IO.Debugging = true;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute);
            engine.Run();
            
            Assert.AreEqual(0, engine.NumOfFoundBugs);
        }
    }
}
