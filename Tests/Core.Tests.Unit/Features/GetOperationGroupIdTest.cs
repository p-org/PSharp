// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.Unit
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
                var id = this.Id.Runtime.GetCurrentOperationGroupId(this.Id);
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
                this.Id.Runtime.SendEvent(this.Id, new E(Id), OperationGroup);
            }

            void CheckEvent()
            {
                var id = this.Id.Runtime.GetCurrentOperationGroupId(Id);
                Assert(id == OperationGroup, $"OperationGroupId is not '{OperationGroup}', but {id}.");
            }
        }

        private void AssertSucceeded(Type machine)
        {
            var configuration = Configuration.Create();
            var runtime = new ProductionRuntime(configuration);
            runtime.SetLogger(new TestOutputLogger(this.TestOutput));
            var failed = false;
            var tcs = new TaskCompletionSource<bool>();
            runtime.OnFailure += delegate
            {
                failed = true;
                tcs.SetResult(true);
            };

            runtime.CreateMachine(machine);

            tcs.Task.Wait(100);
            Assert.False(failed);
        }

        [Fact]
        public void TestGetOperationGroupIdNotSet()
        {
            AssertSucceeded(typeof(M1));
        }

        [Fact]
        public void TestGetOperationGroupIdSet()
        {
            AssertSucceeded(typeof(M2));
        }
    }
}
