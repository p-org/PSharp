//-----------------------------------------------------------------------
// <copyright file="FailureDetectorTest.cs">
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

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    /// <summary>
    /// This test implements a failure detection protocol. A failure detector
    /// machine is given a list of machines, each of which represents a daemon
    /// running at a computing node in a distributed system. The failure detector
    /// sends each machine in the list a 'Ping' event and determines whether the
    /// machine has failed if it does not respond with a 'Pong' event within a
    /// certain time period.
    /// </summary>
    public class FailureDetectorTest : BaseTest
    {
        class Driver : Machine
        {
            internal class Config : Event
            {
                public int NumOfNodes;

                public Config(int numOfNodes)
                {
                    this.NumOfNodes = numOfNodes;
                }
            }

            internal class RegisterClient : Event
            {
                public MachineId Client;

                public RegisterClient(MachineId client)
                {
                    this.Client = client;
                }
            }

            internal class UnregisterClient : Event
            {
                public MachineId Client;

                public UnregisterClient(MachineId client)
                {
                    this.Client = client;
                }
            }

            MachineId FailureDetector;
            HashSet<MachineId> Nodes;
            int NumOfNodes;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.NumOfNodes = (this.ReceivedEvent as Config).NumOfNodes;

                this.Nodes = new HashSet<MachineId>();
                for (int i = 0; i < this.NumOfNodes; i++)
                {
                    var node = this.CreateMachine(typeof(Node));
                    this.Nodes.Add(node);
                }

                this.Monitor<LivenessMonitor>(new LivenessMonitor.RegisterNodes(this.Nodes));

                this.FailureDetector = this.CreateMachine(typeof(FailureDetector), new FailureDetector.Config(this.Nodes));
                this.Send(this.FailureDetector, new RegisterClient(this.Id));

                this.Goto<InjectFailures>();
            }

            [OnEntry(nameof(InjectFailuresOnEntry))]
            [OnEventDoAction(typeof(FailureDetector.NodeFailed), nameof(NodeFailedAction))]
            class InjectFailures : MachineState { }
            
            void InjectFailuresOnEntry()
            {
                foreach (var node in this.Nodes)
                {
                    this.Send(node, new Halt());
                }
            }

            void NodeFailedAction()
            {
                this.Monitor<LivenessMonitor>(this.ReceivedEvent);
            }
        }

        class FailureDetector : Machine
        {
            internal class Config : Event
            {
                public HashSet<MachineId> Nodes;

                public Config(HashSet<MachineId> nodes)
                {
                    this.Nodes = nodes;
                }
            }

            internal class NodeFailed : Event
            {
                public MachineId Node;

                public NodeFailed(MachineId node)
                {
                    this.Node = node;
                }
            }

            class TimerCancelled : Event { }
            class RoundDone : Event { }
            class Unit : Event { }

            HashSet<MachineId> Nodes;
            HashSet<MachineId> Clients;
            int Attempts;
            HashSet<MachineId> Alive;
            HashSet<MachineId> Responses;
            MachineId Timer;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Driver.RegisterClient), nameof(RegisterClientAction))]
            [OnEventDoAction(typeof(Driver.UnregisterClient), nameof(UnregisterClientAction))]
            [OnEventPushState(typeof(Unit), typeof(SendPing))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var nodes = (this.ReceivedEvent as Config).Nodes;

                this.Nodes = new HashSet<MachineId>(nodes);
                this.Clients = new HashSet<MachineId>();
                this.Alive = new HashSet<MachineId>();
                this.Responses = new HashSet<MachineId>();

                foreach (var node in this.Nodes)
                {
                    this.Alive.Add(node);
                }

                this.Timer = this.CreateMachine(typeof(Timer), new Timer.Config(this.Id));
                this.Raise(new Unit());
            }

            void RegisterClientAction()
            {
                var client = (this.ReceivedEvent as Driver.RegisterClient).Client;
                this.Clients.Add(client);
            }

            void UnregisterClientAction()
            {
                var client = (this.ReceivedEvent as Driver.UnregisterClient).Client;
                if (this.Clients.Contains(client))
                {
                    this.Clients.Remove(client);
                }
            }

            [OnEntry(nameof(SendPingOnEntry))]
            [OnEventGotoState(typeof(RoundDone), typeof(Reset))]
            [OnEventPushState(typeof(TimerCancelled), typeof(WaitForCancelResponse))]
            [OnEventDoAction(typeof(Node.Pong), nameof(PongAction))]
            [OnEventDoAction(typeof(Timer.Timeout), nameof(TimeoutAction))]
            class SendPing : MachineState { }

            void SendPingOnEntry()
            {
                foreach (var node in this.Nodes)
                {
                    if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                    {
                        this.Monitor<Safety>(new Safety.Ping(node));
                        this.Send(node, new Node.Ping(this.Id));
                    }
                }

                this.Send(this.Timer, new Timer.StartTimer(100));
            }

            void PongAction()
            {
                var node = (this.ReceivedEvent as Node.Pong).Node;
                if (this.Alive.Contains(node))
                {
                    this.Responses.Add(node);

                    if (this.Responses.Count == this.Alive.Count)
                    {
                        this.Send(this.Timer, new Timer.CancelTimer());
                        this.Raise(new TimerCancelled());
                    }
                }
            }

            void TimeoutAction()
            {
                this.Attempts++;

                if (this.Responses.Count < this.Alive.Count && this.Attempts < 2)
                {
                    this.Goto<SendPing>();
                }
                else
                {
                    foreach (var node in this.Nodes)
                    {
                        if (this.Alive.Contains(node) && !this.Responses.Contains(node))
                        {
                            this.Alive.Remove(node);

                            foreach (var client in this.Clients)
                            {
                                this.Send(client, new NodeFailed(node));
                            }
                        }
                    }

                    this.Raise(new RoundDone());
                }
            }

            [OnEventDoAction(typeof(Timer.CancelSuccess), nameof(CancelSuccessAction))]
            [OnEventDoAction(typeof(Timer.CancelFailure), nameof(CancelFailure))]
            [DeferEvents(typeof(Timer.Timeout), typeof(Node.Pong))]
            class WaitForCancelResponse : MachineState { }

            void CancelSuccessAction()
            {
                this.Raise(new RoundDone());
            }

            void CancelFailure()
            {
                this.Pop();
            }

            [OnEntry(nameof(ResetOnEntry))]
            [OnEventGotoState(typeof(Timer.Timeout), typeof(SendPing))]
            [IgnoreEvents(typeof(Node.Pong))]
            class Reset : MachineState { }

            void ResetOnEntry()
            {
                this.Attempts = 0;
                this.Responses.Clear();

                this.Send(this.Timer, new Timer.StartTimer(1000));
            }
        }

        class Node : Machine
        {
            internal class Ping : Event
            {
                public MachineId Client;

                public Ping(MachineId client)
                {
                    this.Client = client;
                }
            }

            internal class Pong : Event
            {
                public MachineId Node;

                public Pong(MachineId node)
                {
                    this.Node = node;
                }
            }

            [Start]
            [OnEventDoAction(typeof(Ping), nameof(SendPong))]
            class WaitPing : MachineState { }

            void SendPong()
            {
                var client = (this.ReceivedEvent as Ping).Client;
                this.Monitor<Safety>(new Safety.Pong(this.Id));
                this.Send(client, new Pong(this.Id));
            }
        }

        class Timer : Machine
        {
            internal class Config : Event
            {
                public MachineId Target;

                public Config(MachineId target)
                {
                    this.Target = target;
                }
            }

            internal class StartTimer : Event
            {
                public int Timeout;

                public StartTimer(int timeout)
                {
                    this.Timeout = timeout;
                }
            }

            internal class Timeout : Event { }

            internal class CancelSuccess : Event { }
            internal class CancelFailure : Event { }
            internal class CancelTimer : Event { }

            MachineId Target;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Target = (this.ReceivedEvent as Config).Target;
                this.Goto<WaitForReq>();
            }

            [OnEventGotoState(typeof(CancelTimer), typeof(WaitForReq), nameof(CancelTimerAction))]
            [OnEventGotoState(typeof(StartTimer), typeof(WaitForCancel))]
            class WaitForReq : MachineState { }

            void CancelTimerAction()
            {
                this.Send(this.Target, new CancelFailure());
            }

            [IgnoreEvents(typeof(StartTimer))]
            [OnEventGotoState(typeof(CancelTimer), typeof(WaitForReq), nameof(CancelTimerAction2))]
            [OnEventGotoState(typeof(Default), typeof(WaitForReq), nameof(DefaultAction))]
            class WaitForCancel : MachineState { }

            void DefaultAction()
            {
                this.Send(this.Target, new Timeout());
            }

            void CancelTimerAction2()
            {
                if (this.Random())
                {
                    this.Send(this.Target, new CancelSuccess());
                }
                else
                {
                    this.Send(this.Target, new CancelFailure());
                    this.Send(this.Target, new Timeout());
                }
            }
        }

        class Safety : Monitor
        {
            internal class Ping : Event
            {
                public MachineId Client;

                public Ping(MachineId client)
                {
                    this.Client = client;
                }
            }

            internal class Pong : Event
            {
                public MachineId Node;

                public Pong(MachineId node)
                {
                    this.Node = node;
                }
            }

            Dictionary<MachineId, int> Pending;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(Ping), nameof(PingAction))]
            [OnEventDoAction(typeof(Pong), nameof(PongAction))]
            class Init : MonitorState { }

            void InitOnEntry()
            {
                this.Pending = new Dictionary<MachineId, int>();
            }

            void PingAction()
            {
                var client = (this.ReceivedEvent as Ping).Client;
                if (!this.Pending.ContainsKey(client))
                {
                    this.Pending[client] = 0;
                }

                this.Pending[client] = this.Pending[client] + 1;
                this.Assert(this.Pending[client] <= 3, $"'{client}' ping count must be <= 3.");
            }

            void PongAction()
            {
                var node = (this.ReceivedEvent as Pong).Node;
                this.Assert(this.Pending.ContainsKey(node), $"'{node}' is not in pending set.");
                this.Assert(this.Pending[node] > 0, $"'{node}' ping count must be > 0.");
                this.Pending[node] = this.Pending[node] - 1;
            }
        }

        class LivenessMonitor : Monitor
        {
            internal class RegisterNodes : Event
            {
                public HashSet<MachineId> Nodes;

                public RegisterNodes(HashSet<MachineId> nodes)
                {
                    this.Nodes = nodes;
                }
            }

            HashSet<MachineId> Nodes;

            [Start]
            [OnEventDoAction(typeof(RegisterNodes), nameof(RegisterNodesAction))]
            class Init : MonitorState { }

            void RegisterNodesAction()
            {
                var nodes = (this.ReceivedEvent as RegisterNodes).Nodes;
                this.Nodes = new HashSet<MachineId>(nodes);
                this.Goto<Wait>();
            }

            [Hot]
            [OnEventDoAction(typeof(FailureDetector.NodeFailed), nameof(NodeDownAction))]
            class Wait : MonitorState { }

            void NodeDownAction()
            {
                var node = (this.ReceivedEvent as FailureDetector.NodeFailed).Node;
                this.Nodes.Remove(node);
                if (this.Nodes.Count == 0)
                {
                    this.Goto<Done>();
                }
            }

            class Done : MonitorState { }
        }

        [Fact]
        public void TestFailureDetectorSafetyBug()
        {
            var configuration = base.GetConfiguration();
            configuration.MaxUnfairSchedulingSteps = 200;
            configuration.MaxFairSchedulingSteps = 2000;
            configuration.LivenessTemperatureThreshold = 1000;
            configuration.RandomSchedulingSeed = 100813;
            configuration.SchedulingIterations = 1;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(Safety));
                r.CreateMachine(typeof(Driver), new Driver.Config(2));
            });

            var bugReport = "'Microsoft.PSharp.TestingServices.Tests.Integration." +
                "FailureDetectorTest+Node()' ping count must be <= 3.";
            base.AssertFailed(configuration, test, bugReport);
        }

        [Fact]
        public void TestFailureDetectorLivenessBug()
        {
            var configuration = base.GetConfiguration();
            configuration.MaxUnfairSchedulingSteps = 200;
            configuration.MaxFairSchedulingSteps = 2000;
            configuration.LivenessTemperatureThreshold = 1000;
            configuration.RandomSchedulingSeed = 4986;
            configuration.SchedulingIterations = 1;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Driver), new Driver.Config(2));
            });

            var bugReport = "Monitor 'LivenessMonitor' detected potential liveness bug in hot state " +
                "'Microsoft.PSharp.TestingServices.Tests.Integration.FailureDetectorTest+LivenessMonitor.Wait'.";
            base.AssertFailed(configuration, test, bugReport);
        }

        [Fact]
        public void TestFailureDetectorLivenessBugWithCycleReplay()
        {
            var configuration = GetConfiguration();
            configuration.EnableCycleDetection = true;
            configuration.SchedulingStrategy = Utilities.SchedulingStrategy.FairPCT;
            configuration.PrioritySwitchBound = 1;
            configuration.MaxSchedulingSteps = 100;
            configuration.RandomSchedulingSeed = 270;
            configuration.SchedulingIterations = 1;

            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                r.CreateMachine(typeof(Driver), new Driver.Config(2));
            });

            var bugReport = "Monitor 'LivenessMonitor' detected infinite execution that violates a liveness property.";
            AssertFailed(configuration, test, bugReport);
        }
    }
}
