// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.Tests;
using Microsoft.PSharp.Tests.Common;
using Microsoft.PSharp.Timers;
using Xunit.Abstractions;

using BaseCoreTest = Microsoft.PSharp.Core.Tests.BaseTest;
using BaseBugFindingTest = Microsoft.PSharp.TestingServices.Tests.BaseTest;

namespace Microsoft.PSharp.Tests.Launcher
{
    public sealed class CoreTest : BaseCoreTest
    {
        public CoreTest(ITestOutputHelper output)
            : base(output)
        { }

        public async Task Run()
        {
            await Task.CompletedTask;
        }
    }

    public class BugFindingTest : BaseBugFindingTest
    {
        public BugFindingTest(ITestOutputHelper output)
            : base(output)
        { }

        [Test]
        public static void Execute(PSharpRuntime r)
        {
        }
    }

    public static class Assert
    {
        public static void True(bool predicate, string message = null)
        {
            if (!predicate)
            {
                throw new InvalidOperationException(message ?? string.Empty);
            }
        }

        public static void Equal<T>(T expected, T actual) where T : IEquatable<T>
        {
            True(expected.Equals(actual), $"actual '{actual}' != expected '{expected}'");
        }
    }

    public static class Launcher
    {
        private static async Task Main()
        {
            var test = new CoreTest(new TestConsoleLogger());
            await test.Run();
        }
    }
}
