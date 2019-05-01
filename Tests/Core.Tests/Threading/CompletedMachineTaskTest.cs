// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using Microsoft.PSharp.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class CompletedMachineTaskTest : BaseTest
    {
        public CompletedMachineTaskTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCompletedMachineTask()
        {
            MachineTask task = MachineTask.CompletedTask;
            Assert.True(task.IsCompleted);
        }

        [Fact(Timeout = 5000)]
        public void TestCanceledMachineTask()
        {
            CancellationToken token = new CancellationToken(true);
            MachineTask task = MachineTask.FromCanceled(token);
            Assert.True(task.IsCanceled);
        }

        [Fact(Timeout = 5000)]
        public void TestCanceledMachineTaskWithResult()
        {
            CancellationToken token = new CancellationToken(true);
            MachineTask<int> task = MachineTask.FromCanceled<int>(token);
            Assert.True(task.IsCanceled);
        }

        [Fact(Timeout = 5000)]
        public void TestFailedMachineTask()
        {
            MachineTask task = MachineTask.FromException(new InvalidOperationException());
            Assert.True(task.IsFaulted);
            Assert.Equal(typeof(AggregateException), task.Exception.GetType());
            Assert.Equal(typeof(InvalidOperationException), task.Exception.InnerException.GetType());
        }

        [Fact(Timeout = 5000)]
        public void TestFailedMachineTaskWithResult()
        {
            MachineTask<int> task = MachineTask.FromException<int>(new InvalidOperationException());
            Assert.True(task.IsFaulted);
            Assert.Equal(typeof(AggregateException), task.Exception.GetType());
            Assert.Equal(typeof(InvalidOperationException), task.Exception.InnerException.GetType());
        }
    }
}
