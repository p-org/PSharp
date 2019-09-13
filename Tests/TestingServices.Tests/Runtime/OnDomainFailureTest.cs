// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.Timers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class OnDomainFailureTest : BaseTest
    {
        public OnDomainFailureTest(ITestOutputHelper output)
            : base(output)
        {
        }

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
                this.TriggerFailureDomain();
                if (this.FairRandom())
                {
                    if (this.MachineFailureDomain.IsDomainFailed())
                    {
                        this.Assert(false, "Machine {0} should be halted. Reason: The machine domain failed.", this.Id);
                    }

                    this.CreateMachine(typeof(M1));
                }
            }
        }

        private class M1a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(StepUp))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Act();
            }

            private void Act()
            {
                for (int i = 0; i < 5; i++)
                {
                    if (i == 3)
                    {
                        this.TriggerFailureDomain();
                    }

                    if (i == 4)
                    {
                        this.Assert(false, "Machine {0} should be halted. Reason: The machine domain failed.", this.Id);
                    }

                    this.Send(this.Id, new E());
                }
            }

            private void StepUp()
            {
                if (this.MachineFailureDomain.IsDomainFailed())
                {
                    this.Assert(false, "Machine {0} should be halted. Reason: The machine domain failed.", this.Id);
                }
            }
        }

        private class M2a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var testDomain2 = new FailureDomain();
                var mid = this.CreateMachine(typeof(M2b), null, default, new CreateOptions(testDomain2));
                for (int i = 0; i < 5; i++)
                {
                    if (i == 3)
                    {
                        this.Runtime.TriggerFailureDomain(testDomain2);
                    }

                    this.Send(mid, new E());
                }
            }
        }

        private class M2b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }

            private void Act()
            {
                if (this.MachineFailureDomain.IsDomainFailed())
                {
                    this.Assert(false, "Machine {0} should be halted. Reason: The machine domain failed.", this.Id);
                }
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
                this.Id.Runtime.TriggerFailureDomain(null);
                if (this.FairRandom())
                {
                    this.CreateMachine(typeof(M3));
                }
            }
        }

        private class M4a : Machine
        {
            private FailureDomain fd;
            private MachineId childMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act1))]

            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.fd = new FailureDomain();
                this.childMachine = this.CreateMachine(typeof(M4b), new E(this.Id), default, new CreateOptions(this.fd));
            }

            private void Act1()
            {
                this.Id.Runtime.TriggerFailureDomain(this.fd);
                this.Send((this.ReceivedEvent as E).Id, new E(this.Id));
                this.Goto<Active>();
            }

            [OnEntry(nameof(Act2))]
            private class Active : MachineState
            {
            }

            private void Act2()
            {
                if (!this.Runtime.GetMachineFromId<Machine>(this.childMachine).IsHalted && this.Runtime.GetMachineFromId<Machine>(this.childMachine).MachineFailureDomain.DomainFailure)
                {
                    this.Assert(false, "Machine {0} should be halted. Reason: The machine domain failed.", this.childMachine);
                }
            }
        }

        private class M4b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(InitOnEntry))]

            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                if ((this.ReceivedEvent as E).Id != null)
                {
                    this.Send((this.ReceivedEvent as E).Id, new E(this.Id));
                }
            }
        }

        private class M5a : Machine
        {
            private FailureDomain fd;
            private MachineId childMachine;
            private int counter;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]

            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.counter = 0;
                this.fd = new FailureDomain();
                this.childMachine = this.CreateMachine(typeof(M5b), new E(this.Id), default, new CreateOptions(this.fd));
                this.Runtime.SendEvent(this.childMachine, new E(this.Id));
            }

            private void Act()
            {
                if (this.counter == 0)
                {
                    this.Runtime.TriggerFailureDomain(this.fd);
                }

                if (this.counter < 1000)
                {
                    this.counter++;
                    this.Send(this.Id, new E());
                }
                else
                {
                    if (!this.Runtime.GetMachineFromId<Machine>(this.childMachine).IsHalted && this.Runtime.GetMachineFromId<Machine>(this.childMachine).MachineFailureDomain.DomainFailure)
                    {
                        // this.Assert(false, "Machine {0} should be halted. Reason: The machine domain failed.", this.childMachine);
                    }
                }
            }
        }

        private class M5b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]

            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }

            private async Task Act()
            {
                this.Send((this.ReceivedEvent as E).Id, new E());
                await this.Receive(typeof(E));
                await this.Receive(typeof(E));
            }
        }

        private class M6a : Machine
        {
            private MachineId childMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]

            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.childMachine = this.CreateMachine(typeof(M4b), new E(), default, new CreateOptions(new FailureDomain()));
                this.Goto<Active>();
            }

            [OnEntry(nameof(Act))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Active : MachineState
            {
            }

            private async Task Act()
            {
                this.Id.Runtime.TriggerFailureDomain(this.MachineFailureDomain);
                await this.Receive(typeof(E));
                if (!this.IsHalted && this.MachineFailureDomain.DomainFailure)
                {
                    this.Assert(false, "Machine {0} should be halted. Reason: The machine domain failed.", this.Id);
                }

                this.Send(this.childMachine, new E(this.Id));
            }
        }

        private class M7a : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]

            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.CreateMachine(typeof(M7b), null, default, new CreateOptions(new FailureDomain()));
                TimerInfo ti = this.StartTimer(new TimeSpan(1, 0, 0), null);
                this.Id.Runtime.TriggerFailureDomain(this.MachineFailureDomain);
                this.StopTimer(ti);
                if (!this.IsHalted && this.MachineFailureDomain.DomainFailure)
                {
                    this.Assert(false, "Machine {0} should be halted. Reason: The machine domain failed.", this.Id);
                }
            }
        }

        private class M7b : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]

            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Id.Runtime.TriggerFailureDomain(this.MachineFailureDomain);
                this.StartTimer(new TimeSpan(1, 0, 0), null);
                if (!this.IsHalted && this.MachineFailureDomain.DomainFailure)
                {
                    this.Assert(false, "Machine {0} should be halted. Reason: The machine domain failed.", this.Id);
                }
            }
        }

        [Fact(Timeout=5000)]
        public void TestDomainFailureOnCreate()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M1), null, default, new CreateOptions(new FailureDomain()));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestFailurewithError()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            expectedError: "Machine not allocated to failure domain or Null failure domain or failure cannot be triggered.",
            replay: true);
        }

        [Fact(Timeout =5000)]
        public void TestFailureOnSendEvent()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M1a), null, default, new CreateOptions(new FailureDomain()));
            });
        }

        [Fact(Timeout=5000)]
        public void TestFailureOnDequeueEvent()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M2a));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestFailurewithErrorOnTriggerViaRuntime()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3), null, default, new CreateOptions(new FailureDomain()));
            },
            expectedError: "Machine not allocated to failure domain or Null failure domain or failure cannot be triggered.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestFailureOnCreateSentReceive()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M1), null, default, new CreateOptions(new FailureDomain()));
                r.CreateMachine(typeof(M1a), null, default, new CreateOptions(new FailureDomain()));
                r.CreateMachine(typeof(M2a));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestFailureOnCreateSentReceiveWithError()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2a));
                r.CreateMachine(typeof(M1a), null, default, new CreateOptions(new FailureDomain()));
                r.CreateMachine(typeof(M1));
            },
            expectedError: "Machine not allocated to failure domain or Null failure domain or failure cannot be triggered.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestDomainFailureOnEnqueue()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M4a));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestDomainFailureOnWaitingToReceiveEvent()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M6a), null, default, new CreateOptions(new FailureDomain()));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestDomainFailureOnTimers()
        {
            this.Test(r =>
            {
                var m = r.CreateMachine(typeof(M7a), null, default, new CreateOptions(new FailureDomain()));
            });
        }
    }
}
