// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
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

            public E(Guid operationGroupId)
                : base(operationGroupId)
            {
            }

            public E(MachineId id, Guid operationGroupId)
                : base(operationGroupId)
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
                this.Assert(id == Guid.Empty, $"Operation group id is not '{Guid.Empty}', but {id}.");
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
                this.Assert(id == Guid.Empty, $"Operation group id is not '{Guid.Empty}', but {id}.");
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
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
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
                this.Assert(id == Guid.Empty, $"Operation group id is not '{Guid.Empty}', but {id}.");
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
                this.Assert(id == Guid.Empty, $"Operation group id is not '{Guid.Empty}', but {id}.");
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
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
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
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
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
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
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
                this.Assert(id == OperationGroup2, $"Operation group id is not '{OperationGroup2}', but {id}.");
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
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
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
                this.Assert(id == OperationGroup2, $"Operation group id is not '{OperationGroup2}', but {id}.");
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
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
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
                this.Assert(id == OperationGroup1, $"Operation group id is not '{OperationGroup1}', but {id}.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingSingleMachineNoSend()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1));
            });
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingSingleMachineSend()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M2));
            });
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingSingleMachineSendStarter()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M2S));
            });
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesCreate()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M3));
            });
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSend()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M5));
            });
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSendStarter()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M5S));
            });
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSendBack()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M7));
            });
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingTwoMachinesSendBackStarter()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M7S));
            });
        }

        [Fact(Timeout=5000)]
        public void TestOperationGroupingThreeMachinesSendStarter()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M9S));
            });
        }
    }
}
