//-----------------------------------------------------------------------
// <copyright file="SEMTwoMachines4Test.cs">
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
    public class SEMTwoMachines4Test
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
        class PingIgnored : Event { }

        class PING : Machine
        {
            MachineId PongId;
            int Count;

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
                Count = Count + 1;
                if (Count == 1)
                {
                    this.Send(PongId, new Ping(this.Id));
                }
                // halt PONG after one exchange
                if (Count == 2)
                {
                    this.Send(PongId, new Halt());
                    this.Send(PongId, new PingIgnored());
                }

                this.Raise(new Success());
            }

            [OnEventGotoState(typeof(Pong), typeof(SendPing))]
            class WaitPong : MachineState { }

            class Done : MachineState { }
        }

        class PONG : Machine
        {
            [Start]
            [OnEventGotoState(typeof(Ping), typeof(SendPong))]
            [OnEventGotoState(typeof(Halt), typeof(PongHalt))]
            class WaitPing : MachineState { }

            [OnEntry(nameof(EntrySendPong))]
            [OnEventGotoState(typeof(Success), typeof(WaitPing))]
            class SendPong : MachineState { }

            void EntrySendPong()
            {
                this.Send((this.ReceivedEvent as Ping).Id, new Pong());
                this.Raise(new Success());
            }

            [OnEventDoAction(typeof(PingIgnored), nameof(Action1))]
            [IgnoreEvents(typeof(Ping))]
            class PongHalt : MachineState { }

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
        /// Tests that an event sent to a machine after it received the
        /// "halt" event is ignored by the halted machine.
        /// Case when "halt" is explicitly handled.
        /// </summary>
        [TestMethod]
        public void TestEventSentAfterSentHaltHandled()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var engine = TestingEngineFactory.CreateBugFindingEngine(
                configuration, TestProgram.Execute);
            engine.Run();

            Assert.AreEqual(1, engine.TestReport.NumOfFoundBugs);
        }
    }
}
