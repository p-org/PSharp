// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.TestingServices.Tests;
using Microsoft.PSharp.Tests.Common;
using Microsoft.PSharp.Timers;
using Xunit.Abstractions;

using BaseBugFindingTest = Microsoft.PSharp.TestingServices.Tests.BaseTest;
using BaseCoreTest = Microsoft.PSharp.Core.Tests.BaseTest;

namespace Microsoft.PSharp.Tests.Launcher
{
    public sealed class CoreTest : BaseCoreTest
    {
        internal class E : Event
        {
            public MachineId Id;

            public E(MachineId id)
            {
                this.Id = id;
            }
        }

        internal class Unit : Event
        {
        }

        internal class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var n = this.CreateMachine(typeof(N));
                this.Send(n, new E(this.Id));
            }

            private void Act()
            {
                this.Assert(false);
            }
        }

        internal class N : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void Act()
            {
                MachineId m = (this.ReceivedEvent as E).Id;
                this.Send(m, new E(this.Id));
            }
        }

        public CoreTest(ITestOutputHelper output)
            : base(output)
        {
        }

#pragma warning disable CA1822 // Mark members as static
        public async Task Run()
        {
            Configuration configuration = GetConfiguration();
            BugFindingEngine engine = BugFindingEngine.Create(configuration,
                r =>
                {
                    // CustomLogFormatter logFormatter = new CustomLogFormatter();
                    // r.SetLogFormatter(logFormatter);
                    r.CreateMachine(typeof(M));
                });

            // var logger = new Common.TestOutputLogger(this.TestOutput, true);

            try
            {
                // engine.SetLogger(logger);
                engine.Run();

                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.True(engine.ReadableTrace != null, "Readable trace is null.");
                Assert.True(engine.ReadableTrace.Length > 0, "Readable trace is empty.");
            }
            catch (Exception ex)
            {
            }

            await Task.CompletedTask;
        }
#pragma warning restore CA1822 // Mark members as static
    }

    public class BugFindingTest : BaseBugFindingTest
    {
        public BugFindingTest(ITestOutputHelper output)
            : base(output)
        {
        }

#pragma warning disable CA1801 // Parameter not used
        [Test]
        public static void Execute(IMachineRuntime r)
        {
            r.CreateMachine(typeof(CoreTest.M));
        }
#pragma warning restore CA1801 // Parameter not used
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
            var test = new CoreTest(new TestConsoleLogger(true));
            await test.Run();
        }
    }
}
