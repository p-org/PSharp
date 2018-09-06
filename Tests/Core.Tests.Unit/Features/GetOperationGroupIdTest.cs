﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.PSharp.Core.Tests.Unit
{
    public class GetOperationGroupIdTest 
    {
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

        private void AssertSucceeded(Type machine)
        {
            var runtime = PSharpRuntime.Create();
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
