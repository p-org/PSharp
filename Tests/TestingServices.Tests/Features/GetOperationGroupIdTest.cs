// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public class GetOperationGroupIdTest : BaseTest
    {
        public GetOperationGroupIdTest(ITestOutputHelper output)
            : base(output)
        { }

        static Guid OperationGroup = Guid.NewGuid();

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
                var id = Runtime.GetCurrentOperationGroupId(Id);
                Assert(id == Guid.Empty, $"OperationGroupId is not '{Guid.Empty}', but {id}.");
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
                Runtime.SendEvent(Id, new E(Id), OperationGroup);
            }

            void CheckEvent()
            {
                var id = Runtime.GetCurrentOperationGroupId(Id);
                Assert(id == OperationGroup, $"OperationGroupId is not '{OperationGroup}', but {id}.");
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
                Runtime.GetCurrentOperationGroupId(target);
            }
        }

        class M4 : Machine
        {
            [Start]
            class Init : MachineState { }
        }

        [Fact]
        public void TestGetOperationGroupIdNotSet()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M1));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestGetOperationGroupIdSet()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M2));
            });

            AssertSucceeded(test);
        }

        [Fact]
        public void TestGetOperationGroupIdOfNotCurrentMachine()
        {
            var test = new Action<PSharpRuntime>((r) => {
                r.CreateMachine(typeof(M3));
            });

            string bugReport = "Trying to access the operation group id of 'M4()', " +
                "which is not the currently executing machine.";
            AssertFailed(test, bugReport, true);
        }
    }
}
