// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.PSharp.Runtime;
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

                    this.Send(this.Id, new E());
                }
            }

            private void StepUp()
            {
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
    }
}
