// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class OperationGroupingTest : BaseTest
    {
        public OperationGroupingTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private static Guid OperationGroup1 = Guid.NewGuid();
        private static Guid OperationGroup2 = Guid.NewGuid();

        private class E : Event
        {
            public MachineId Id;

            public E()
            {
            }

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        private class M1 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == Guid.Empty, $"NextOperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void CheckEvent()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == Guid.Empty, $"NextOperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        private class M2S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Runtime.SendEvent(this.Id, new E(), OperationGroup1);
            }

            private void CheckEvent()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M4));
            }
        }

        private class M4 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == Guid.Empty, $"NextOperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M6));
                this.Send(target, new E());
            }
        }

        private class M6 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == Guid.Empty, $"NextOperationGroupId is not '{Guid.Empty}', but {id}.");
            }
        }

        private class M5S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M6S));
                this.Runtime.SendEvent(target, new E(), OperationGroup1);
            }
        }

        private class M6S : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        private class M7 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M8));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
            }

            private void CheckEvent()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        private class M8 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
                this.Send((this.ReceivedEvent as E).Id, new E());
            }
        }

        private class M7S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M8S));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
            }

            private void CheckEvent()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == OperationGroup2, $"NextOperationGroupId is not '{OperationGroup2}', but {id}.");
            }
        }

        private class M8S : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
                this.Runtime.SendEvent((this.ReceivedEvent as E).Id, new E(), OperationGroup2);
            }
        }

        private class M9S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var target = this.CreateMachine(typeof(M10S));
                this.Runtime.SendEvent(target, new E(this.Id), OperationGroup1);
            }

            private void CheckEvent()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == OperationGroup2, $"NextOperationGroupId is not '{OperationGroup2}', but {id}.");
            }
        }

        private class M10S : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(CheckEvent))]
            private class Init : MachineState
            {
            }

            private void CheckEvent()
            {
                var target = this.CreateMachine(typeof(M11S));
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
                this.Runtime.SendEvent((this.ReceivedEvent as E).Id, new E(), OperationGroup2);
            }
        }

        private class M11S : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var id = (this.Info as ISchedulable).NextOperationGroupId;
                this.Assert(id == OperationGroup1, $"NextOperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingSingleMachineNoSend()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M1));
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingSingleMachineSend()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2));
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingSingleMachineSendStarter()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M2S));
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesCreate()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M3));
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSend()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M5));
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSendStarter()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M5S));
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSendBack()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M7));
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSendBackStarter()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M7S));
            });

            this.AssertSucceeded(test);
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingThreeMachinesSendStarter()
        {
            var test = new Action<IMachineRuntime>((r) =>
            {
                r.CreateMachine(typeof(M9S));
            });

            this.AssertSucceeded(test);
        }
    }
}
