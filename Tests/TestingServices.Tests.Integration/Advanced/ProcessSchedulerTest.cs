//-----------------------------------------------------------------------
// <copyright file="ProcessSchedulerTest.cs">
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
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    /// <summary>
    /// A single-process implementation of a process scheduling algorithm.
    /// </summary>
    public class ProcessSchedulerTest : BaseTest
    {
        public enum MType
        {
            WakeUp,
            Run
        }

        class Environment : Machine
        {
            [Start]
            [OnEntry(nameof(OnInitEntry))]
            class Init : MachineState { }

            void OnInitEntry()
            {
                var lkMachine = CreateMachine(typeof(LkMachine));
                var rLockMachine = CreateMachine(typeof(RLockMachine));
                var rWantMachine = CreateMachine(typeof(RWantMachine));
                var nodeMachine = CreateMachine(typeof(Node));
                CreateMachine(typeof(Client), new Client.Configure(lkMachine, rLockMachine, rWantMachine, nodeMachine));
                CreateMachine(typeof(Server), new Server.Configure(lkMachine, rLockMachine, rWantMachine, nodeMachine));
            }
        }

        class Server : Machine
        {
            public class Configure : Event
            {
                public MachineId LKMachineId;
                public MachineId RLockMachineId;
                public MachineId RWantMachineId;
                public MachineId NodeMachineId;

                public Configure(MachineId lkMachineId, MachineId rLockMachineId,
                    MachineId rWantMachineId, MachineId nodeMachineId)
                {
                    LKMachineId = lkMachineId;
                    RLockMachineId = rLockMachineId;
                    RWantMachineId = rWantMachineId;
                    NodeMachineId = nodeMachineId;
                }
            }

            public class Wakeup : Event { }

            private MachineId LKMachineId;
            private MachineId RLockMachineId;
            private MachineId RWantMachineId;
            public MachineId NodeMachineId;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(Wakeup), nameof(OnWakeup))]
            class Init : MachineState { }

            void OnInitialize()
            {
                var e = ReceivedEvent as Configure;
                LKMachineId = e.LKMachineId;
                RLockMachineId = e.RLockMachineId;
                RWantMachineId = e.RWantMachineId;
                NodeMachineId = e.NodeMachineId;
                Raise(new Wakeup());
            }

            void OnWakeup()
            {
                Send(RLockMachineId, new RLockMachine.SetReq(Id, false));
                Receive(typeof(RLockMachine.SetResp)).Wait();
                Send(LKMachineId, new LkMachine.Waiting(Id, false));
                Receive(typeof(LkMachine.WaitResp)).Wait();
                Send(RWantMachineId, new RWantMachine.ValueReq(Id));
                var receivedEvent = Receive(typeof(RWantMachine.ValueResp)).Result;

                if ((receivedEvent as RWantMachine.ValueResp).Value == true)
                {
                    Send(RWantMachineId, new RWantMachine.SetReq(Id, false));
                    Receive(typeof(RWantMachine.SetResp)).Wait();

                    Send(NodeMachineId, new Node.ValueReq(Id));
                    var receivedEvent1 = Receive(typeof(Node.ValueResp)).Result;
                    if ((receivedEvent1 as Node.ValueResp).Value == MType.WakeUp)
                    {
                        Send(NodeMachineId, new Node.SetReq(Id, MType.Run));
                        Receive(typeof(Node.SetResp)).Wait();
                    }
                }

                Send(Id, new Wakeup());
            }
        }

        class Client : Machine
        {
            public class Configure : Event
            {
                public MachineId LKMachineId;
                public MachineId RLockMachineId;
                public MachineId RWantMachineId;
                public MachineId NodeMachineId;

                public Configure(MachineId lkMachineId, MachineId rLockMachineId,
                    MachineId rWantMachineId, MachineId nodeMachineId)
                {
                    LKMachineId = lkMachineId;
                    RLockMachineId = rLockMachineId;
                    RWantMachineId = rWantMachineId;
                    NodeMachineId = nodeMachineId;
                }
            }

            public class Sleep : Event { }
            public class Progress : Event { }

            private MachineId LKMachineId;
            private MachineId RLockMachineId;
            private MachineId RWantMachineId;
            public MachineId NodeMachineId;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(Sleep), nameof(OnSleep))]
            [OnEventDoAction(typeof(Progress), nameof(OnProgress))]
            class Init : MachineState { }

            void OnInitialize()
            {
                var e = ReceivedEvent as Configure;
                LKMachineId = e.LKMachineId;
                RLockMachineId = e.RLockMachineId;
                RWantMachineId = e.RWantMachineId;
                NodeMachineId = e.NodeMachineId;
                Raise(new Progress());
            }

            void OnSleep()
            {
                Send(LKMachineId, new LkMachine.AtomicTestSet(this.Id));
                Receive(typeof(LkMachine.AtomicTestSet_Resp)).Wait();
                while (true)
                {
                    Send(RLockMachineId, new RLockMachine.ValueReq(this.Id));
                    var receivedEvent = Receive(typeof(RLockMachine.ValueResp)).Result;
                    if ((receivedEvent as RLockMachine.ValueResp).Value == true)
                    {
                        Send(RWantMachineId, new RWantMachine.SetReq(this.Id, true));
                        Receive(typeof(RWantMachine.SetResp)).Wait();
                        Send(NodeMachineId, new Node.SetReq(this.Id, MType.WakeUp));
                        Receive(typeof(Node.SetResp)).Wait();
                        Send(LKMachineId, new LkMachine.SetReq(this.Id, false));
                        Receive(typeof(LkMachine.SetResp)).Wait();

                        this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientSleep());

                        Send(NodeMachineId, new Node.Waiting(this.Id, MType.Run));
                        Receive(typeof(Node.WaitResp)).Wait();

                        this.Monitor<LivenessMonitor>(new LivenessMonitor.NotifyClientProgress());
                    }
                    else
                    {
                        break;
                    }
                }

                Send(Id, new Progress());
            }

            void OnProgress()
            {
                Send(RLockMachineId, new RLockMachine.ValueReq(Id));
                var receivedEvent = Receive(typeof(RLockMachine.ValueResp)).Result;
                this.Assert((receivedEvent as RLockMachine.ValueResp).Value == false);
                Send(RLockMachineId, new RLockMachine.SetReq(this.Id, true));
                Receive(typeof(RLockMachine.SetResp)).Wait();
                Send(LKMachineId, new LkMachine.SetReq(this.Id, false));
                Receive(typeof(LkMachine.SetResp)).Wait();
                Send(Id, new Sleep());
            }
        }

        class Node : Machine
        {
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
                public MType Value;

                public ValueResp(MType value)
                {
                    Value = value;
                }
            }

            public class SetReq : Event
            {
                public MachineId Target;
                public MType Value;

                public SetReq(MachineId target, MType value)
                {
                    Target = target;
                    Value = value;
                }
            }

            public class SetResp : Event { }

            public class Waiting : Event
            {
                public MachineId Target;
                public MType WaitingOn;

                public Waiting(MachineId target, MType waitingOn)
                {
                    Target = target;
                    WaitingOn = waitingOn;
                }
            }

            public class WaitResp : Event { }

            private MType State;
            private Dictionary<MachineId, MType> blockedMachines;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
            [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
            class Init : MachineState { }

            void OnInitialize()
            {
                State = MType.Run;
                blockedMachines = new Dictionary<MachineId, MType>();
            }

            void OnSetReq()
            {
                var e = ReceivedEvent as SetReq;
                State = e.Value;
                Unblock();
                Send(e.Target, new SetResp());
            }

            void OnValueReq()
            {
                var e = ReceivedEvent as ValueReq;
                Send(e.Target, new ValueResp(State));
            }

            void OnWaiting()
            {
                var e = ReceivedEvent as Waiting;
                if (State == e.WaitingOn)
                {
                    Send(e.Target, new WaitResp());
                }
                else
                {
                    blockedMachines.Add(e.Target, e.WaitingOn);
                }
            }

            void Unblock()
            {
                List<MachineId> remove = new List<MachineId>();
                foreach (var target in blockedMachines.Keys)
                {
                    if (blockedMachines[target] == State)
                    {
                        Send(target, new WaitResp());
                        remove.Add(target);
                    }
                }

                foreach (var key in remove)
                {
                    blockedMachines.Remove(key);
                }
            }
        }

        class LkMachine : Machine
        {
            public class AtomicTestSet : Event
            {
                public MachineId Target;

                public AtomicTestSet(MachineId target)
                {
                    Target = target;
                }
            }

            public class AtomicTestSet_Resp : Event { }

            public class SetReq : Event
            {
                public MachineId Target;
                public bool Value;

                public SetReq(MachineId target, bool value)
                {
                    Target = target;
                    Value = value;
                }
            }

            public class SetResp : Event { }

            public class Waiting : Event
            {
                public MachineId Target;
                public bool WaitingOn;

                public Waiting(MachineId target, bool waitingOn)
                {
                    Target = target;
                    WaitingOn = waitingOn;
                }
            }

            public class WaitResp : Event { }

            private bool LK;
            private Dictionary<MachineId, bool> BlockedMachines;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(AtomicTestSet), nameof(OnAtomicTestSet))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(Waiting), nameof(OnWaiting))]
            class Init : MachineState { }

            void OnInitialize()
            {
                LK = false;
                BlockedMachines = new Dictionary<MachineId, bool>();
            }

            void OnAtomicTestSet()
            {
                var e = ReceivedEvent as AtomicTestSet;
                if (LK == false)
                {
                    LK = true;
                    Unblock();
                }
                Send(e.Target, new AtomicTestSet_Resp());
            }

            void OnSetReq()
            {
                var e = ReceivedEvent as SetReq;
                LK = e.Value;
                Unblock();
                Send(e.Target, new SetResp());
            }

            void OnWaiting()
            {
                var e = ReceivedEvent as Waiting;
                if (LK == e.WaitingOn)
                {
                    Send(e.Target, new WaitResp());
                }
                else
                {
                    BlockedMachines.Add(e.Target, e.WaitingOn);
                }
            }

            void Unblock()
            {
                List<MachineId> remove = new List<MachineId>();
                foreach (var target in BlockedMachines.Keys)
                {
                    if (BlockedMachines[target] == LK)
                    {
                        Send(target, new WaitResp());
                        remove.Add(target);
                    }
                }

                foreach (var key in remove)
                {
                    BlockedMachines.Remove(key);
                }
            }
        }

        class RLockMachine : Machine
        {
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
                public bool Value;

                public ValueResp(bool value)
                {
                    Value = value;
                }
            }

            public class SetReq : Event
            {
                public MachineId Target;
                public bool Value;

                public SetReq(MachineId target, bool value)
                {
                    Target = target;
                    Value = value;
                }
            }

            public class SetResp : Event { }

            private bool RLock;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
            class Init : MachineState { }

            void OnInitialize()
            {
                RLock = false;
            }

            void OnSetReq()
            {
                var e = ReceivedEvent as SetReq;
                RLock = e.Value;
                Send(e.Target, new SetResp());
            }

            void OnValueReq()
            {
                var e = ReceivedEvent as ValueReq;
                Send(e.Target, new ValueResp(RLock));
            }
        }

        class RWantMachine : Machine
        {
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
                public bool Value;

                public ValueResp(bool value)
                {
                    Value = value;
                }
            }

            public class SetReq : Event
            {
                public MachineId Target;
                public bool Value;

                public SetReq(MachineId target, bool value)
                {
                    Target = target;
                    Value = value;
                }
            }

            public class SetResp : Event { }

            private bool RWant;

            [Start]
            [OnEntry(nameof(OnInitialize))]
            [OnEventDoAction(typeof(SetReq), nameof(OnSetReq))]
            [OnEventDoAction(typeof(ValueReq), nameof(OnValueReq))]
            class Init : MachineState { }

            void OnInitialize()
            {
                RWant = false;
            }

            void OnSetReq()
            {
                var e = ReceivedEvent as SetReq;
                RWant = e.Value;
                Send(e.Target, new SetResp());
            }

            void OnValueReq()
            {
                var e = ReceivedEvent as ValueReq;
                Send(e.Target, new ValueResp(RWant));
            }
        }

        class LivenessMonitor : Monitor
        {
            public class NotifyClientSleep : Event { }
            public class NotifyClientProgress : Event { }

            [Start]
            [OnEntry(nameof(InitOnEntry))]

            class Init : MonitorState { }

            [Hot]
            [OnEventGotoState(typeof(NotifyClientProgress), typeof(Progressing))]
            class Suspended : MonitorState { }

            [Cold]
            [OnEventGotoState(typeof(NotifyClientSleep), typeof(Suspended))]
            class Progressing : MonitorState { }

            void InitOnEntry()
            {
                Goto(typeof(Progressing));
            }
        }

        [Fact]
        public void TestProcessSchedulerLivenessBugWithCycleReplay()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingStrategy = Utilities.SchedulingStrategy.FairPCT;
            configuration.PrioritySwitchBound = 1;
            configuration.MaxUnfairSchedulingSteps = 100;
            configuration.MaxFairSchedulingSteps = 1000;
            configuration.LivenessTemperatureThreshold = 500;
            configuration.RandomSchedulingSeed = 684;
            configuration.SchedulingIterations = 1;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Environment));
            });

            var bugReport = "Monitor 'LivenessMonitor' detected infinite execution that violates a liveness property.";
            AssertFailed(configuration, test, bugReport);
        }
    }
}
