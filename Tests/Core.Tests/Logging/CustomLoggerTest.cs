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

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests
{
    public class CustomLoggerTest : BaseTest
    {
        public CustomLoggerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class CustomLogger : MachineLogger
        {
            private StringBuilder StringBuilder;

            public CustomLogger()
            {
                this.StringBuilder = new StringBuilder();
            }

            public override void Write(string value)
            {
                this.StringBuilder.Append(value);
            }

            public override void Write(string format, params object[] args)
            {
                this.StringBuilder.AppendFormat(format, args);
            }

            public override void WriteLine(string value)
            {
                this.StringBuilder.AppendLine(value);
            }

            public override void WriteLine(string format, params object[] args)
            {
                this.StringBuilder.AppendFormat(format, args);
                this.StringBuilder.AppendLine();
            }

            public override string ToString()
            {
                return this.StringBuilder.ToString();
            }

            public override void Dispose()
            {
                this.StringBuilder.Clear();
                this.StringBuilder = null;
            }
        }

        internal class Configure : Event
        {
            public TaskCompletionSource<bool> TCS;

            public Configure(TaskCompletionSource<bool> tcs)
            {
                this.TCS = tcs;
            }
        }

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

        private class M : Machine
        {
            private TaskCompletionSource<bool> TCS;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Act))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.TCS = (this.ReceivedEvent as Configure).TCS;
                var n = this.CreateMachine(typeof(N));
                this.Send(n, new E(this.Id));
            }

            private void Act()
            {
                this.TCS.SetResult(true);
            }
        }

        private class N : Machine
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

        [Fact]
        public void TestCustomLogger()
        {
            CustomLogger logger = new CustomLogger();

            Configuration config = Configuration.Create().WithVerbosityEnabled(2);
            var runtime = PSharpRuntime.Create(config);
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));
            tcs.Task.Wait();

            string expected = @"<CreateLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M()' was created by the runtime.
<StateLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M()' enters state 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M.Init'.
<ActionLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M()' in state 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M.Init' invoked action 'InitOnEntry'.
<CreateLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N()' was created by machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M()'.
<StateLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N()' enters state 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N.Init'.
<SendLog> Operation Group <none>: Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M()' in state 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M.Init' sent event 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+E' to machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N()'.
<EnqueueLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N()' enqueued event 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+E'.
<DequeueLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N()' in state 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N.Init' dequeued event 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+E'.
<ActionLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N()' in state 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N.Init' invoked action 'Act'.
<SendLog> Operation Group <none>: Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N()' in state 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+N.Init' sent event 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+E' to machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M()'.
<EnqueueLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M()' enqueued event 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+E'.
<DequeueLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M()' in state 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M.Init' dequeued event 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+E'.
<ActionLog> Machine 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M()' in state 'Microsoft.PSharp.Core.Tests.CustomLoggerTest+M.Init' invoked action 'Act'.
";
            string actual = Regex.Replace(logger.ToString(), "[0-9]", string.Empty);

            HashSet<string> expectedSet = new HashSet<string>(Regex.Split(expected, "\r\n|\r|\n"));
            HashSet<string> actualSet = new HashSet<string>(Regex.Split(actual, "\r\n|\r|\n"));

            Assert.True(expectedSet.SetEquals(actualSet));

            logger.Dispose();
        }

        [Fact]
        public void TestCustomLoggerNoVerbosity()
        {
            CustomLogger logger = new CustomLogger();

            var runtime = PSharpRuntime.Create();
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));
            tcs.Task.Wait();

            Assert.Equal(string.Empty, logger.ToString());

            logger.Dispose();
        }

        [Fact]
        public void TestNullCustomLoggerFail()
        {
            var config = GetConfiguration().WithVerbosityEnabled(2);
            var test = new Action<IMachineRuntime>((r) =>
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => r.SetLogger(null));
                Assert.Equal("Cannot install a null logger.", ex.Message);
            });

            this.Run(config, test);
        }
    }
}
