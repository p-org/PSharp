// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime;
using Microsoft.PSharp.TestingServices.Tests;
using Microsoft.PSharp.TestingServices.Threading;
using Microsoft.PSharp.Tests.Common;
using Microsoft.PSharp.Threading;
using Microsoft.PSharp.Timers;
using Xunit.Abstractions;

using BaseBugFindingTest = Microsoft.PSharp.TestingServices.Tests.BaseTest;
using BaseCoreTest = Microsoft.PSharp.Core.Tests.BaseTest;

namespace Microsoft.PSharp.Tests.Launcher
{
#pragma warning disable SA1005 // Single line comments must begin with single space
#pragma warning disable CA1801 // Parameter not used
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA2200 // Rethrow to preserve stack details.
    public sealed class CoreTest : BaseCoreTest
    {
        public CoreTest(ITestOutputHelper output)
            : base(output)
        {
        }

        public async Task Run()
        {
            async MachineTask func()
            {
                await MachineTask.Delay(10);
                //throw new InvalidOperationException();
            }

            var task = MachineTask.Run(func);

            MachineTask innerTask = null;
            Exception exception = null;
            try
            {
#pragma warning disable IDE0067 // Dispose objects before losing scope
                innerTask = await task;
#pragma warning restore IDE0067 // Dispose objects before losing scope
                await innerTask;
            }
            catch (Exception ex)
            {
                exception = ex;
                Console.WriteLine("Ex>>: " + ex);
            }

            Console.WriteLine("1>>: " + exception);
            Console.WriteLine("2>>: " + task.Status);
            Console.WriteLine("3>>: " + innerTask.Status);

            await Task.CompletedTask;
        }
    }

    public class BugFindingTest : BaseBugFindingTest
    {
        public BugFindingTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Test]
        public static async MachineTask Execute(IMachineRuntime r)
        {
            throw new InvalidOperationException();
        }
    }

    public static class Assert
    {
        public static void True(bool predicate, string message = null)
        {
            Specification.Assert(predicate, message ?? string.Empty);
        }

        public static void Equal<T>(T expected, T actual)
            where T : IEquatable<T>
        {
            True(expected.Equals(actual), $"actual '{actual}' != expected '{expected}'");
        }
    }

    public static class Program
    {
        private static async Task Main()
        {
            var test = new CoreTest(new TestConsoleLogger());
            await test.Run();
        }
    }
#pragma warning restore CA2200 // Rethrow to preserve stack details.
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CA1801 // Parameter not used
#pragma warning restore SA1005 // Single line comments must begin with single space
}
