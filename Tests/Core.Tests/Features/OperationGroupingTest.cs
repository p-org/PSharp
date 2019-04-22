// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
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

            public E(Guid operationGroup)
                : base(operationGroup)
            {
            }

            public E(MachineId id, Guid operationGroup)
                : base(operationGroup)
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
                var id = this.OperationGroupId;
                this.Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
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
                var id = this.OperationGroupId;
                this.Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
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
                this.Runtime.SendEvent(this.Id, new E(OperationGroup1));
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
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
                this.CreateMachine(typeof(M4));
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
                var id = this.OperationGroupId;
                this.Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
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
                var id = this.OperationGroupId;
                this.Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
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
                this.Runtime.SendEvent(target, new E(OperationGroup1));
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
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
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
                this.Runtime.SendEvent(target, new E(this.Id, OperationGroup1));
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
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
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
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
                this.Runtime.SendEvent(target, new E(this.Id, OperationGroup1));
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup2, $"OperationGroupId is not '{OperationGroup2}', but {id}.");
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
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
                this.Runtime.SendEvent((this.ReceivedEvent as E).Id, new E(OperationGroup2));
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
                this.Runtime.SendEvent(target, new E(this.Id, OperationGroup1));
            }

            private void CheckEvent()
            {
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup2, $"OperationGroupId is not '{OperationGroup2}', but {id}.");
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
                this.CreateMachine(typeof(M11S));
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
                this.Runtime.SendEvent((this.ReceivedEvent as E).Id, new E(OperationGroup2));
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
                var id = this.OperationGroupId;
                this.Assert(id == OperationGroup1, $"OperationGroupId is not '{OperationGroup1}', but {id}.");
            }
        }

        private void AssertSucceeded(Type machine)
        {
            var config = GetConfiguration();
            var test = new Action<IMachineRuntime>((r) =>
            {
                var failed = false;
                var tcs = new TaskCompletionSource<bool>();
                r.OnFailure += (ex) =>
                {
                    failed = true;
                    tcs.SetResult(true);
                };

                r.CreateMachine(machine);

                tcs.Task.Wait(100);
                Assert.False(failed);
            });

            this.Run(config, test);
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingSingleMachineNoSend()
        {
            this.AssertSucceeded(typeof(M1));
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingSingleMachineSend()
        {
            this.AssertSucceeded(typeof(M2));
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingSingleMachineSendStarter()
        {
            this.AssertSucceeded(typeof(M2S));
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesCreate()
        {
            this.AssertSucceeded(typeof(M3));
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSend()
        {
            this.AssertSucceeded(typeof(M5));
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSendStarter()
        {
            this.AssertSucceeded(typeof(M5S));
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSendBack()
        {
            this.AssertSucceeded(typeof(M7));
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSendBackStarter()
        {
            this.AssertSucceeded(typeof(M7S));
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingThreeMachinesSendStarter()
        {
            this.AssertSucceeded(typeof(M9S));
        }
    }
}
