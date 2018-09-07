// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Utilities;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Integration
{
    public class DPORTest : BaseTest
    {
        class Ping : Event { }

        class SenderInitEvent : Event
        {
            public readonly MachineId WaiterMachineId;
            public readonly bool SendPing;
            public readonly bool DoNonDet;

            public SenderInitEvent(MachineId waiter, bool sendPing = false, bool doNonDet = false)
            {
                WaiterMachineId = waiter;
                SendPing = sendPing;
                DoNonDet = doNonDet;
            }
        }

        class InitEvent : Event { }
        class DummyEvent : Event { }

        class Waiter : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Ping), nameof(Nothing))]
            private class Init : MachineState { }

            private void Nothing() { }
        }

        class Sender : Machine
        {
            private SenderInitEvent initEvent;

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(Ping), nameof(SendPing))]
            [OnEventDoAction(typeof(DummyEvent), nameof(Nothing))]
            private class Init : MachineState { }

            private void Initialize()
            {
                initEvent = ((SenderInitEvent)ReceivedEvent);
            }

            private void SendPing()
            {
                if (initEvent.SendPing)
                {
                    Send(initEvent.WaiterMachineId, new Ping());
                }

                if (initEvent.DoNonDet)
                {
                    Random();
                    Random();
                }

                Send(Id, new DummyEvent());
            }

            private void Nothing() { }
        }

        class ReceiverAddressEvent : Event
        {
            public readonly MachineId Receiver;

            public ReceiverAddressEvent(MachineId receiver)
            {
                Receiver = receiver;
            }
        }

        class LevelOne : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState { }

            private void Initialize()
            {
                var r = (ReceiverAddressEvent) ReceivedEvent;
                CreateMachine(typeof(LevelTwo), r);
                CreateMachine(typeof(LevelTwo), r);
            }
        }

        class LevelTwo : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState { }

            private void Initialize()
            {
                var r = (ReceiverAddressEvent) ReceivedEvent;
                var a = CreateMachine(typeof(Sender),
                    new SenderInitEvent(r.Receiver, true));
                Send(a, new Ping());
                var b = CreateMachine(typeof(Sender),
                    new SenderInitEvent(r.Receiver, true));
                Send(b, new Ping());
            }
        }

        class ReceiveWaiter : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState { }

            private async Task Initialize()
            {
                await Receive(typeof(Ping));
                await Receive(typeof(Ping));
            }
        }

        [Fact]
        public void TestDPOR1Reduces()
        {
            var test = new Action<IPSharpRuntime>(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(Waiter));
                MachineId sender1 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                MachineId sender2 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                MachineId sender3 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                r.SendEvent(sender1, new Ping());
                r.SendEvent(sender2, new Ping());
                r.SendEvent(sender3, new Ping());
            });

            var configuration = GetConfiguration();
            configuration.SchedulingIterations = 10;

            // DPOR: 1 schedule.
            configuration.SchedulingStrategy = SchedulingStrategy.DPOR;
            var runtime = AssertSucceeded(configuration, test);
            Assert.Equal(1, runtime.TestReport.NumOfExploredUnfairSchedules);

            // DFS: at least 6 schedules.
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            runtime = AssertSucceeded(configuration, test);
            Assert.True(runtime.TestReport.NumOfExploredUnfairSchedules >= 6);
        }

        [Fact]
        public void TestDPOR2NonDet()
        {
            var test = new Action<IPSharpRuntime>(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(Waiter));
                MachineId sender1 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter, false, true));
                MachineId sender2 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                MachineId sender3 = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter));
                r.SendEvent(sender1, new Ping());
                r.SendEvent(sender2, new Ping());
                r.SendEvent(sender3, new Ping());
            });

            var configuration = GetConfiguration();
            configuration.SchedulingIterations = 10;

            // DPOR: 4 schedules because there are 2 nondet choices.
            configuration.SchedulingStrategy = SchedulingStrategy.DPOR;
            var runtime = AssertSucceeded(configuration, test);
            Assert.Equal(4, runtime.TestReport.NumOfExploredUnfairSchedules);
        }

        [Fact]
        public void TestDPOR3CreatingMany()
        {
            var test = new Action<IPSharpRuntime>(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(Waiter));
                r.CreateMachine(typeof(LevelOne),
                    new ReceiverAddressEvent(waiter));
                r.CreateMachine(typeof(LevelOne),
                    new ReceiverAddressEvent(waiter));
            });

            var configuration = GetConfiguration();
            configuration.SchedulingIterations = 10;

            configuration.SchedulingStrategy = SchedulingStrategy.DPOR;
            AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestDPOR4UseReceive()
        {
            var test = new Action<IPSharpRuntime>(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(ReceiveWaiter));

                var a = r.CreateMachine(typeof(Sender),
                    new SenderInitEvent(waiter, true));
                r.SendEvent(a, new Ping());
                var b = r.CreateMachine(typeof(Sender),
                    new SenderInitEvent(waiter, true));
                r.SendEvent(b, new Ping());
            });

            var configuration = GetConfiguration();
            configuration.SchedulingIterations = 1000;

            configuration.SchedulingStrategy = SchedulingStrategy.DPOR;
            AssertSucceeded(configuration, test);
        }
    }
}
