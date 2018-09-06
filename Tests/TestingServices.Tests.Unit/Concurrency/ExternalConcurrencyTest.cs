// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public class ExternalConcurrencyTest : BaseTest
    {
        class E : Event { }

        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                Task task = Task.Run(() => {
                    this.Send(this.Id, new E());
                });
                task.Wait();
            }
        }

        class N : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                Task task = Task.Run(() => {
                    this.Random();
                });
                task.Wait();
            }
        }

        [Fact]
        public void TestExternalTaskSendingEvent()
        {
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(M)); });
            string bugReport = @"Detected task with id '' that is not controlled by the P# runtime.";
            base.AssertFailed(test, bugReport, true);
        }

        [Fact]
        public void TestExternalTaskInvokingRandom()
        {
            var test = new Action<PSharpRuntime>((r) => { r.CreateMachine(typeof(N)); });
            string bugReport = @"Detected task with id '' that is not controlled by the P# runtime.";
            base.AssertFailed(test, bugReport, true);
        }
    }
}
