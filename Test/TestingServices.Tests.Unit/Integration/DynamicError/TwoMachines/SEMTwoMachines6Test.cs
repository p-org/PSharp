//-----------------------------------------------------------------------
// <copyright file="SEMTwoMachines6Test.cs">
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
    public class SEMTwoMachines6Test
    {
        class Ping : Event
        {
            public MachineId Id;
            public Ping(MachineId id) : base(1, -1) { this.Id = id; }
        }

        class Pong : Event
        {
            public Pong() : base(1, -1) { }
        }

        class Success : Event { }

        class PING : Machine
        {
            MachineId PongId;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnEventGotoState(typeof(Success), typeof(SendPing))]
            class Init : MachineState { }

            void EntryInit()
            {
                PongId = this.CreateMachine(typeof(PONG));
                this.Raise(new Success());
            }

            [OnEntry(nameof(EntrySendPing))]
            [OnEventGotoState(typeof(Success), typeof(WaitPong))]
            class SendPing : MachineState { }

            void EntrySendPing()
            {
                this.Send(PongId, new Ping(this.Id));
                this.Raise(new Success());
            }

            [OnEventGotoState(typeof(Pong), typeof(SendPing))]
            class WaitPong : MachineState { }

            class Done : MachineState { }
        }

        class PONG : Machine
        {
            int Count2 = 0;

            [Start]
            [OnEntry(nameof(EntryWaitPing))]
            [OnEventGotoState(typeof(Ping), typeof(SendPong))]
            class WaitPing : MachineState { }

            void EntryWaitPing() { }

            [OnEntry(nameof(EntrySendPong))]
            [OnEventGotoState(typeof(Success), typeof(WaitPing))]
            [OnEventDoAction(typeof(Halt), nameof(Action1))]
            class SendPong : MachineState { }

            void EntrySendPong()
            {
                Count2 = Count2 + 1;

                if (Count2 == 1)
                {
                    this.Send((this.ReceivedEvent as Ping).Id, new Pong());
                }

                if (Count2 == 2)
                {
                    this.Send((this.ReceivedEvent as Ping).Id, new Pong());
                    this.Raise(new Halt());
                    return; // important if not compiling
                }

                this.Raise(new Success());
            }

            void Action1()
            {
                this.Assert(false); // reachable
            }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(PING));
            }
        }

        /// <summary>
        ///  P# semantics test: two machines, machine is halted with "raise halt"
        /// (handled). This test is for the case when "halt" is explicitly handled
        /// - hence, it is processed as any other event.
        /// </summary>
        [TestMethod]
        public void TestRaisedHaltHandled()
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
