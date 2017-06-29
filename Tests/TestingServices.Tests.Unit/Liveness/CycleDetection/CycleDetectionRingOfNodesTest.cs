//-----------------------------------------------------------------------
// <copyright file="CycleDetectionRingOfNodesTest.cs">
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

using System;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class CycleDetectionRingOfNodesTest : BaseTest
    {
        class Configure: Event
        {
            public bool ApplyFix;

            public Configure(bool applyFix)
            {
                this.ApplyFix = applyFix;
            }
        }

        class Message : Event { }

        class Environment : Machine
        {
            [Start]
            [OnEntry(nameof(OnInitEntry))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                var applyFix = (this.ReceivedEvent as Configure).ApplyFix;
                var machine1 = this.CreateMachine(typeof(Node), new Configure(applyFix));
                var machine2 = this.CreateMachine(typeof(Node), new Configure(applyFix));
                this.Send(machine1, new Node.SetNeighbour(machine2));
                this.Send(machine2, new Node.SetNeighbour(machine1));
            }
        }

        class Node : Machine
        {
            public class SetNeighbour : Event
            {
                public MachineId Next;

                public SetNeighbour(MachineId next)
                {
                    this.Next = next;
                }
            }

            MachineId Next;
            bool ApplyFix;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(SetNeighbour), nameof(OnSetNeighbour))]
            [OnEventDoAction(typeof(Message), nameof(OnMessage))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                this.ApplyFix = (this.ReceivedEvent as Configure).ApplyFix;
            }

            void OnSetNeighbour()
            {
                var e = ReceivedEvent as SetNeighbour;
                this.Next = e.Next;
                this.Send(this.Id, new Message());
            }

            void OnMessage()
            {
                if (Next != null)
                {
                    this.Send(Next, new Message());
                    if (this.ApplyFix)
                    {
                        this.Monitor<WatchDog>(new WatchDog.NotifyMessage());
                    }
                }
            }
        }

        class WatchDog : Monitor
        {
            public class NotifyMessage : Event { }

            [Start]
            [Hot]
            [OnEventGotoState(typeof(NotifyMessage), typeof(ColdState))]
            class HotState : MonitorState { }

            [Cold]
            [OnEventGotoState(typeof(NotifyMessage), typeof(HotState))]
            class ColdState : MonitorState { }
        }

        [Fact]
        public void TestCycleDetectionRingOfNodesNoBug()
        {
            var configuration = base.GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingIterations = 10;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(Environment), new Configure(true));
            });

            base.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestCycleDetectionRingOfNodesBug()
        {
            var configuration = base.GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.MaxSchedulingSteps = 200;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(WatchDog));
                r.CreateMachine(typeof(Environment), new Configure(false));
            });

            string bugReport = "Monitor 'WatchDog' detected infinite execution that violates a liveness property.";
            base.AssertFailed(configuration, test, bugReport);
        }
    }
}
