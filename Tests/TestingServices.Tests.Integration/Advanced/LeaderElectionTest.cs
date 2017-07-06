//-----------------------------------------------------------------------
// <copyright file="LeaderElectionTest.cs">
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

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    /// <summary>
    /// This is a simple implementation of a leader election protocol.
    /// </summary>
    public class LeaderElectionTest : BaseTest
    {
        public enum MsgType
        {
            One,
            Two,
            Winner
        }

        class Environment : Machine
        {
            [Start]
            [OnEntry(nameof(OnInitEntry))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                var leaderCountMachine = CreateMachine(typeof(LeaderCount_Machine));
                var node1 = CreateMachine(typeof(Node), new Node.Configure(leaderCountMachine, 1));
                var node2 = CreateMachine(typeof(Node), new Node.Configure(leaderCountMachine, 2));
                var node3 = CreateMachine(typeof(Node), new Node.Configure(leaderCountMachine, 3));
                var node4 = CreateMachine(typeof(Node), new Node.Configure(leaderCountMachine, 4));
                var node5 = CreateMachine(typeof(Node), new Node.Configure(leaderCountMachine, 5));

                Send(node1, new Node.SetNeighbours(node2, node5));
                Send(node2, new Node.SetNeighbours(node3, node1));
                Send(node3, new Node.SetNeighbours(node4, node2));
                Send(node4, new Node.SetNeighbours(node5, node3));
                Send(node5, new Node.SetNeighbours(node1, node4));
            }
        }

        class LeaderCount_Machine : Machine
        {
            public class UpdateLeadercount : Event { }

            public class ValueReq : Event
            {
                public MachineId Target;

                public ValueReq(MachineId target)
                {
                    Target = target;
                }
            }

            public class ValueResp : Event
            {
                public int Value;

                public ValueResp(int value)
                {
                    Value = value;
                }
            }

            int NrLeaders;

            [Start]
            [OnEntry(nameof(OnInitEntry))]
            [OnEventDoAction(typeof(UpdateLeadercount), nameof(OnUpdateLeaderCount))]
            [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                NrLeaders = 0;
            }

            void OnUpdateLeaderCount()
            {
                NrLeaders++;
            }

            void OnValueReq()
            {
                var e = ReceivedEvent as ValueReq;
                Send(e.Target, new ValueResp(NrLeaders));
            }

            protected override int HashedState
            {
                get
                {
                    int hash = 19;
                    hash = hash * 31 + NrLeaders;
                    return hash;
                }
            }
        }

        public class Node : Machine
        {
            public class Configure : Event
            {
                public int MyNumber;
                public MachineId LeaderCount_MachineId;

                public Configure(MachineId leaderCount_MachineId, int myNumber)
                {
                    MyNumber = myNumber;
                    LeaderCount_MachineId = leaderCount_MachineId;
                }
            }

            public class SetNeighbours : Event
            {
                public MachineId InputMachineId;
                public MachineId OutputMachineId;

                public SetNeighbours(MachineId inputMachineId, MachineId outputMachineId)
                {
                    InputMachineId = inputMachineId;
                    OutputMachineId = outputMachineId;
                }
            }

            public class StartElection : Event { }

            public class Message : Event
            {
                public MsgType MsgType;
                public int Nr;

                public Message(MsgType msgType, int nr)
                {
                    MsgType = msgType;
                    Nr = nr;
                }
            }

            public class ContinueElection : Event { }

            private MachineId InputMachineId;
            private MachineId OutputMachineId;
            private MachineId LeaderCountMachineId;
            private int MyNumber;

            private bool Active;
            private bool KnowWinner;

            private int Maximum;
            private int NeighbourR;

            [Start]
            [OnEntry(nameof(OnInit))]
            [OnEventDoAction(typeof(SetNeighbours), nameof(OnSetNeighbours))]
            [OnEventDoAction(typeof(StartElection), nameof(OnStartElection))]
            [OnEventGotoState(typeof(ContinueElection), typeof(SecondPhase))]
            [DeferEvents(typeof(Message))]
            class Init : MachineState { }

            [OnEntry(nameof(OnContinueElection))]
            class SecondPhase : MachineState { }

            void OnInit()
            {
                var e = ReceivedEvent as Configure;
                MyNumber = e.MyNumber;
                Active = true;
                KnowWinner = false;
                Maximum = MyNumber;
                LeaderCountMachineId = e.LeaderCount_MachineId;
            }

            void OnSetNeighbours()
            {
                var e = ReceivedEvent as SetNeighbours;
                InputMachineId = e.InputMachineId;
                OutputMachineId = e.OutputMachineId;
                Raise(new StartElection());
            }

            void OnStartElection()
            {
                Runtime.Logger.WriteLine($"[MSG ({Id.Name})] My Number: " + MyNumber);
                Runtime.Logger.WriteLine($"[LOG ({Id.Name})] Sent one, {MyNumber} to {OutputMachineId.Name}");
                Send(OutputMachineId, new Message(MsgType.One, MyNumber));
                Send(Id, new ContinueElection());
            }

            void OnContinueElection()
            {
                while (true)
                {
                    var receivedEvent = Receive(typeof(Message)).Result;
                    var receivedMsgType = (receivedEvent as Message).MsgType;
                    var receivedNr = (receivedEvent as Message).Nr;

                    Runtime.Logger.WriteLine($"[LOG ({Id.Name})] Received: " + receivedMsgType + "; " + receivedNr);

                    if (receivedMsgType == MsgType.One)
                    {
                        if (Active)
                        {
                            if (receivedNr != Maximum)
                            {
                                Runtime.Logger.WriteLine($"[LOG ({Id.Name})] Sent two, {receivedNr} to {OutputMachineId.Name}");
                                Send(OutputMachineId, new Message(MsgType.Two, receivedNr));
                                NeighbourR = receivedNr;
                            }
                            else
                            {
                                Assert(receivedNr == 5);
                                KnowWinner = true;
                                Runtime.Logger.WriteLine($"[LOG ({Id.Name})] Sent winner, {receivedNr} to {OutputMachineId.Name}");
                                Send(OutputMachineId, new Message(MsgType.Winner, receivedNr));
                            }
                        }
                        else
                        {
                            Runtime.Logger.WriteLine($"[LOG ({Id.Name})] Sent one, {receivedNr} to {OutputMachineId.Name}");
                            Send(OutputMachineId, new Message(MsgType.One, receivedNr));
                        }
                    }
                    else if (receivedMsgType == MsgType.Two)
                    {
                        if (Active)
                        {
                            if (NeighbourR > receivedNr && NeighbourR > Maximum)
                            {
                                Maximum = NeighbourR;
                                Runtime.Logger.WriteLine($"[LOG ({Id.Name})] Sent one, {NeighbourR} to {OutputMachineId.Name}");
                                Send(OutputMachineId, new Message(MsgType.One, NeighbourR));
                            }
                            else
                            {
                                Active = false;
                            }
                        }
                        else
                        {
                            Runtime.Logger.WriteLine($"[LOG ({Id.Name})] Sent two, {receivedNr} to {OutputMachineId.Name}");
                            Send(OutputMachineId, new Message(MsgType.Two, receivedNr));
                        }
                    }
                    else if (receivedMsgType == MsgType.Winner)
                    {
                        if (receivedNr != MyNumber)
                        {
                            Runtime.Logger.WriteLine($"[MSG ({Id.Name})] Lost");
                        }
                        else
                        {
                            Runtime.Logger.WriteLine($"[MSG ({Id.Name})] Leader");
                            Send(LeaderCountMachineId, new LeaderCount_Machine.UpdateLeadercount());
                            Send(LeaderCountMachineId, new LeaderCount_Machine.ValueReq(Id));
                            var receivedEvent1 = Receive(typeof(LeaderCount_Machine.ValueResp)).Result;
                            Assert((receivedEvent1 as LeaderCount_Machine.ValueResp).Value == 1);
                            Monitor<LivenessMonitor>(new LivenessMonitor.NotifyLeaderElected());
                        }
                        if (!KnowWinner)
                        {
                            Runtime.Logger.WriteLine($"[LOG ({Id.Name})] Sent winner, {receivedNr} to {OutputMachineId.Name}");
                            Send(OutputMachineId, new Message(MsgType.Winner, receivedNr));
                        }
                        break;
                    }
                }
            }

            protected override int HashedState
            {
                get
                {
                    int hash = 19;
                    hash = hash * 31 + Active.GetHashCode();
                    hash = hash * 31 + InputMachineId.GetHashCode();
                    hash = hash * 31 + KnowWinner.GetHashCode();
                    hash = hash * 31 + LeaderCountMachineId.GetHashCode();
                    hash = hash * 31 + Maximum;
                    hash = hash * 31 + MyNumber;
                    hash = hash * 31 + NeighbourR;
                    hash = hash * 31 + OutputMachineId.GetHashCode();
                    return hash;
                }
            }
        }

        class LivenessMonitor : Monitor
        {
            public class NotifyLeaderElected : Event { }

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MonitorState { }

            [Hot]
            [OnEventGotoState(typeof(NotifyLeaderElected), typeof(LeaderElected))]
            class NoLeaderElected : MonitorState { }

            [Cold]
            class LeaderElected : MonitorState { }

            void InitOnEntry()
            {
                Goto(typeof(NoLeaderElected));
            }
        }

        [Fact]
        public void TestLeaderElectionProtocol()
        {
            var configuration = GetConfiguration();
            configuration.SchedulingStrategy = Utilities.SchedulingStrategy.FairPCT;
            configuration.PrioritySwitchBound = 1;
            configuration.MaxSchedulingSteps = 100;
            configuration.SchedulingIterations = 100;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Environment));
            });

            AssertSucceeded(configuration, test);
        }
    }
}
