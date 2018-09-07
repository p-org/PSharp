// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class OperationGroupingTest : BaseTest
    {
        public OperationGroupingTest(ITestOutputHelper output)
            : base(output)
        { }

        static Guid OperationGroup1 = Guid.NewGuid();
        static Guid OperationGroup2 = Guid.NewGuid();

        class E : Event
        {
            public MachineId Id;

            public E() { }

            public E(MachineId id)
            {
                Id = id;
            }
        }

        class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == Guid.Empty, $"NextOperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                Send(Id, new E());
            }

            void CheckEvent()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == Guid.Empty, $"NextOperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        class M2S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                Runtime.SendEvent(Id, new E(), OperationGroup1);
            }

            void CheckEvent()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M4));
            }
        }

        class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == Guid.Empty, $"NextOperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M6));
                Send(target, new E());
            }
        }

        class M6 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == Guid.Empty, $"NextOperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        class M5S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M6S));
                Runtime.SendEvent(target, new E(), OperationGroup1);
            }
        }

        class M6S : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        class M7 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M8));
                Runtime.SendEvent(target, new E(Id), OperationGroup1);
            }

            void CheckEvent()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        class M8 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
                Send((ReceivedEvent as E).Id, new E());
            }
        }

        class M7S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M8S));
                Runtime.SendEvent(target, new E(Id), OperationGroup1);
            }

            void CheckEvent()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == OperationGroup2, $"NextOperationGroupId is not '{OperationGroup2}', but {id}.");
            }
        }

        class M8S : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
                Runtime.SendEvent((ReceivedEvent as E).Id, new E(), OperationGroup2);
            }
        }

        class M9S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var target = CreateMachine(typeof(M10S));
                Runtime.SendEvent(target, new E(Id), OperationGroup1);
            }

            void CheckEvent()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == OperationGroup2, $"NextOperationGroupId is not '{OperationGroup2}', but {id}.");
            }
        }

        class M10S : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            class Init : MachineState { }

            void CheckEvent()
            {
                var target = CreateMachine(typeof(M11S));
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
                Runtime.SendEvent((ReceivedEvent as E).Id, new E(), OperationGroup2);
            }
        }

        class M11S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            class Init : MachineState { }

            void InitOnEntry()
            {
                var id = (Info as ISchedulable).NextOperationGroupId;
                Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        [Fact]
        public void TestOperationGroupingSingleMachineNoSend()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestOperationGroupingSingleMachineSend()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestOperationGroupingSingleMachineSendStarter()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2S));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesCreate()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M3));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesSend()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M5));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesSendStarter()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M5S));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesSendBack()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M7));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestOperationGroupingTwoMachinesSendBackStarter()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M7S));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestOperationGroupingThreeMachinesSendStarter()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M9S));
            });

            AssertSucceeded(test);
        }
    }
}
