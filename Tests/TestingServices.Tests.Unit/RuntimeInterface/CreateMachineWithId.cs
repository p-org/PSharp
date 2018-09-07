﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class CreateMachineWithId : BaseTest
    {
        public CreateMachineWithId(ITestOutputHelper output)
            : base(output)
        { }

        class E : Event { }

        class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E), typeof(S2))]
            class S1 : MonitorState { }

            [Hot]
            [OnEventGotoState(typeof(E), typeof(S3))]
            class S2 : MonitorState { }

            [Cold]
            class S3 : MonitorState { }
        }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                this.Monitor(typeof(LivenessMonitor), new E());
            }
        }

        [Fact]
        public void TestCreateWithId1()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m = r.CreateMachine(typeof(M));
                var mprime = r.CreateMachineId(typeof(M));
                r.Assert(m != mprime);
                r.CreateMachine(mprime, typeof(M));
            });

            base.AssertSucceeded(test);
        }

        class Data
        {
            public int x;

            public Data()
            {
                x = 0;
            }
        }

        class E1 : Event
        {
            public Data data;

            public E1(Data data)
            {
                this.data = data;
            }
        }

        class TerminateReq : Event
        {
            public MachineId sender;
            public TerminateReq(MachineId sender)
            {
                this.sender = sender;
            }
        }

        class TerminateResp : Event { }

        class M1 : Machine
        {
            Data data;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Process))]
            [OnEventDoAction(typeof(TerminateReq), nameof(Terminate))]
            class S : MachineState { }

            void InitOnEntry()
            {
                data = (this.ReceivedEvent as E1).data;
                Process();
            }

            void Process()
            {
                if (data.x != 10)
                {
                    data.x++;
                    this.Send(this.Id, new E());
                }
                else
                {
                    this.Monitor(typeof(LivenessMonitor), new E());
                    this.Monitor(typeof(LivenessMonitor), new E());
                }

            }

            void Terminate()
            {
                this.Send((this.ReceivedEvent as TerminateReq).sender, new TerminateResp());
                this.Raise(new Halt());
            }
        }

        class Harness : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class S : MachineState { }

            void InitOnEntry()
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
        public void TestCreateWithId2()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m = r.CreateMachine(typeof(Harness));
            });

            base.AssertSucceeded(test);
        }

        class M2 : Machine
        {
            [Start]
            class S : MachineState { }
        }

        class M3 : Machine
        {
            [Start]
            class S : MachineState { }
        }

        [Fact]
        public void TestCreateWithId3()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m3 = r.CreateMachineId(typeof(M3));
                r.CreateMachine(m3, typeof(M2));
            });

            base.AssertFailed(test, "Cannot bind machine id '' of type 'Microsoft.PSharp.TestingServices.Tests.Unit.CreateMachineWithId+M3' to a machine of type 'Microsoft.PSharp.TestingServices.Tests.Unit.CreateMachineWithId+M2'.", true);
        }

        [Fact]
        public void TestCreateWithId4()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m2 = r.CreateMachine(typeof(M2));
                r.CreateMachine(m2, typeof(M2));
            });

            base.AssertFailed(test, "Machine with id '' is already bound to an existing machine.", true);
        }

        [Fact]
        public void TestCreateWithId5()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachineId(typeof(M2));
                r.SendEvent(m, new E());
            });

            base.AssertFailed(test, "Cannot Send event Microsoft.PSharp.TestingServices.Tests.Unit.CreateMachineWithId+E to a MachineId '' that was never previously bound to a machine of type Microsoft.PSharp.TestingServices.Tests.Unit.CreateMachineWithId+M2()", true);
        }

        [Fact]
        public void TestCreateWithId6()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachine(typeof(M2));

                // Make sure that the machine halts
                for (int i = 0; i < 100; i++)
                {
                    r.SendEvent(m, new Halt());
                }

                // trying to bring up a halted machine
                r.CreateMachine(m, typeof(M2));
            });

            base.AssertFailed(test, "MachineId '' of a previously halted machine cannot be reused to create a new machine of type Microsoft.PSharp.TestingServices.Tests.Unit.CreateMachineWithId+M2", false);
        }

        class E2: Event
        {
            public MachineId mid;

            public E2(MachineId mid)
            {
                this.mid = mid;
            }
        }

        class M4 : Machine
        {
            [Start]
            [IgnoreEvents(typeof(E))]
            [OnEntry(nameof(InitOnEntry))]
            class S : MachineState { }

            void InitOnEntry()
            {

            }
        }

        class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class S : MachineState { }

            void InitOnEntry()
            {
                var mid = (this.ReceivedEvent as E2).mid;
                this.Send(mid, new E());
            }
        }

        [Fact]
        public void TestCreateWithId7()
        {
            var test = new Action<PSharpRuntime>((r) => {
                var m = r.CreateMachineId(typeof(M4));
                r.CreateMachine(typeof(M5), new E2(m));
                r.CreateMachine(m, typeof(M4));
            });

            var config = Configuration.Create().WithNumberOfIterations(100);
            config.ReductionStrategy = Utilities.ReductionStrategy.None;

            base.AssertFailed(config, test, "Cannot Send event Microsoft.PSharp.TestingServices.Tests.Unit.CreateMachineWithId+E to a MachineId '' that was never previously bound to a machine of type Microsoft.PSharp.TestingServices.Tests.Unit.CreateMachineWithId+M4()", false);
        }
    }
}
