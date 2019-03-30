// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Utilities;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class DPORTest : BaseTest
    {
        public DPORTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Ping : Event
        {
        }

        private class SenderInitEvent : Event
        {
            public readonly MachineId WaiterMachineId;
            public readonly bool SendPing;
            public readonly bool DoNonDet;

            public SenderInitEvent(MachineId waiter, bool sendPing = false, bool doNonDet = false)
            {
                this.WaiterMachineId = waiter;
                this.SendPing = sendPing;
                this.DoNonDet = doNonDet;
            }
        }

        private class InitEvent : Event
        {
        }

        private class DummyEvent : Event
        {
        }

        private class Waiter : Machine
        {
            [Start]
            [OnEventDoAction(typeof(Ping), nameof(Nothing))]
            private class Init : MachineState
            {
            }

            private void Nothing()
            {
            }
        }

        private class Sender : Machine
        {
            private SenderInitEvent initEvent;

            [Start]
            [OnEntry(nameof(Initialize))]
            [OnEventDoAction(typeof(Ping), nameof(SendPing))]
            [OnEventDoAction(typeof(DummyEvent), nameof(Nothing))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                this.initEvent = (SenderInitEvent)this.ReceivedEvent;
            }

            private void SendPing()
            {
                if (this.initEvent.SendPing)
                {
                    this.Send(this.initEvent.WaiterMachineId, new Ping());
                }

                if (this.initEvent.DoNonDet)
                {
                    this.Random();
                    this.Random();
                }

                this.Send(this.Id, new DummyEvent());
            }

            private void Nothing()
            {
            }
        }

        private class ReceiverAddressEvent : Event
        {
            public readonly MachineId Receiver;

            public ReceiverAddressEvent(MachineId receiver)
            {
                this.Receiver = receiver;
            }
        }

        private class LevelOne : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                var r = (ReceiverAddressEvent)this.ReceivedEvent;
                this.CreateMachine(typeof(LevelTwo), r);
                this.CreateMachine(typeof(LevelTwo), r);
            }
        }

        private class LevelTwo : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private void Initialize()
            {
                var r = (ReceiverAddressEvent)this.ReceivedEvent;
                var a = this.CreateMachine(typeof(Sender), new SenderInitEvent(r.Receiver, true));
                this.Send(a, new Ping());
                var b = this.CreateMachine(typeof(Sender), new SenderInitEvent(r.Receiver, true));
                this.Send(b, new Ping());
            }
        }

        private class ReceiveWaiter : Machine
        {
            [Start]
            [OnEntry(nameof(Initialize))]
            private class Init : MachineState
            {
            }

            private async Task Initialize()
            {
                await this.Receive(typeof(Ping));
                await this.Receive(typeof(Ping));
            }
        }

        [Fact]
        public void TestDPOR1Reduces()
        {
            var test = new Action<PSharpRuntime>(r =>
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
            var runtime = this.AssertSucceeded(configuration, test);
            Assert.Equal(1, runtime.TestReport.NumOfExploredUnfairSchedules);

            // DFS: at least 6 schedules.
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;
            runtime = this.AssertSucceeded(configuration, test);
            Assert.True(runtime.TestReport.NumOfExploredUnfairSchedules >= 6);
        }

        [Fact]
        public void TestDPOR2NonDet()
        {
            var test = new Action<PSharpRuntime>(r =>
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
            var runtime = this.AssertSucceeded(configuration, test);
            Assert.Equal(4, runtime.TestReport.NumOfExploredUnfairSchedules);
        }

        [Fact]
        public void TestDPOR3CreatingMany()
        {
            var test = new Action<PSharpRuntime>(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(Waiter));
                r.CreateMachine(typeof(LevelOne), new ReceiverAddressEvent(waiter));
                r.CreateMachine(typeof(LevelOne), new ReceiverAddressEvent(waiter));
            });

            var configuration = GetConfiguration();
            configuration.SchedulingIterations = 10;

            configuration.SchedulingStrategy = SchedulingStrategy.DPOR;
            this.AssertSucceeded(configuration, test);
        }

        [Fact]
        public void TestDPOR4UseReceive()
        {
            var test = new Action<PSharpRuntime>(r =>
            {
                MachineId waiter = r.CreateMachine(typeof(ReceiveWaiter));

                var a = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter, true));
                r.SendEvent(a, new Ping());
                var b = r.CreateMachine(typeof(Sender), new SenderInitEvent(waiter, true));
                r.SendEvent(b, new Ping());
            });

            var configuration = GetConfiguration();
            configuration.SchedulingIterations = 1000;

            configuration.SchedulingStrategy = SchedulingStrategy.DPOR;
            this.AssertSucceeded(configuration, test);
        }
    }
}
