// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class CreateMachineWithIdTest : BaseTest
    {
        public CreateMachineWithIdTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E), typeof(S2))]
            private class S1 : MonitorState
            {
            }

            [Hot]
            [OnEventGotoState(typeof(E), typeof(S3))]
            private class S2 : MonitorState
            {
            }

            [Cold]
            private class S3 : MonitorState
            {
            }
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Monitor(typeof(LivenessMonitor), new E());
            }
        }

        [Fact]
        public void TestCreateMachineWithId1()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m = r.CreateMachine(typeof(M));
                var mprime = r.CreateMachineId(typeof(M));
                r.Assert(m != mprime);
                r.CreateMachine(mprime, typeof(M));
            });

            this.AssertSucceeded(test);
        }

        private class Data
        {
            public int X;

            public Data()
            {
                this.X = 0;
            }
        }

        private class E1 : Event
        {
            public Data Data;

            public E1(Data data)
            {
                this.Data = data;
            }
        }

        private class TerminateReq : Event
        {
            public MachineId Sender;

            public TerminateReq(MachineId sender)
            {
                this.Sender = sender;
            }
        }

        private class TerminateResp : Event
        {
        }

        private class M1 : Machine
        {
            private Data data;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Process))]
            [OnEventDoAction(typeof(TerminateReq), nameof(Terminate))]
            private class S : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.data = (this.ReceivedEvent as E1).Data;
                this.Process();
            }

            private void Process()
            {
                if (this.data.X != 10)
                {
                    this.data.X++;
                    this.Send(this.Id, new E());
                }
                else
                {
                    this.Monitor(typeof(LivenessMonitor), new E());
                    this.Monitor(typeof(LivenessMonitor), new E());
                }
            }

            private void Terminate()
            {
                this.Send((this.ReceivedEvent as TerminateReq).Sender, new TerminateResp());
                this.Raise(new Halt());
            }
        }

        private class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : MachineState
            {
            }

            private void InitOnEntry()
            {
                var data = new Data();
                var m1 = this.CreateMachine(typeof(M1), new E1(data));
                var m2 = this.Id.Runtime.CreateMachineId(typeof(M1));
                this.Send(m1, new TerminateReq(this.Id));
                this.Receive(typeof(TerminateResp));
                this.Id.Runtime.CreateMachine(m2, typeof(M1), new E1(data));
            }
        }

        [Fact]
        public void TestCreateMachineWithId2()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m = r.CreateMachine(typeof(Harness));
            });

            this.AssertSucceeded(test);
        }

        private class M2 : Machine
        {
            [Start]
            private class S : MachineState
            {
            }
        }

        private class M3 : Machine
        {
            [Start]
            private class S : MachineState
            {
            }
        }

        [Fact]
        public void TestCreateMachineWithId3()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                var m3 = r.CreateMachineId(typeof(M3));
                r.CreateMachine(m3, typeof(M2));
            });

            this.AssertFailed(test, "Cannot bind machine id '' of type 'M3' to a machine of type 'M2'.", true);
        }

        [Fact]
        public void TestCreateMachineWithId4()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                var m2 = r.CreateMachine(typeof(M2));
                r.CreateMachine(m2, typeof(M2));
            });

            this.AssertFailed(test, "Machine with id '' is already bound to an existing machine.", true);
        }

        [Fact]
        public void TestCreateMachineWithId5()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                var m = r.CreateMachineId(typeof(M2));
                r.SendEvent(m, new E());
            });

            this.AssertFailed(test, "Cannot send event 'E' to machine id '' that was never " +
                "previously bound to a machine of type 'M2'", true);
        }

        [Fact]
        public void TestCreateMachineWithId6()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                var m = r.CreateMachine(typeof(M2));

                // Make sure that the machine halts
                for (int i = 0; i < 100; i++)
                {
                    r.SendEvent(m, new Halt());
                }

                // trying to bring up a halted machine
                r.CreateMachine(m, typeof(M2));
            });

            this.AssertFailed(test, "MachineId '' of a previously halted machine cannot be " +
                "reused to create a new machine of type 'M2'", false);
        }

        private class E2 : Event
        {
            public MachineId Mid;

            public E2(MachineId mid)
            {
                this.Mid = mid;
            }
        }

        private class M4 : Machine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            [OnEntry(nameof(InitOnEntry))]
            private class S : MachineState
            {
            }

            private void InitOnEntry()
            {
            }
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : MachineState
            {
            }

            private void InitOnEntry()
            {
                var mid = (this.ReceivedEvent as E2).Mid;
                this.Send(mid, new E());
            }
        }

        [Fact]
        public void TestCreateMachineWithId7()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                var m = r.CreateMachineId(typeof(M4));
                r.CreateMachine(typeof(M5), new E2(m));
                r.CreateMachine(m, typeof(M4));
            });

            var config = Configuration.Create().WithNumberOfIterations(100);
            config.ReductionStrategy = Utilities.ReductionStrategy.None;

            this.AssertFailed(config, test, "Cannot send event 'E' to machine id '' that was never " +
                "previously bound to a machine of type 'M4'", false);
        }
    }
}
