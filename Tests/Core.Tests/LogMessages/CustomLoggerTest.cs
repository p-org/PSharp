﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.LogMessages
{
    public class CustomLoggerTest : BaseTest
    {
        public CustomLoggerTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public async Task TestCustomLogger()
        {
            CustomLogger logger = new CustomLogger(true);

            Configuration config = Configuration.Create().WithVerbosityEnabled();
            var runtime = PSharpRuntime.Create(config);
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));

            await WaitAsync(tcs.Task);
            await Task.Delay(200);

            string expected = @"<CreateLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.M()' was created by the runtime.
<StateLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.M()' enters state 'Init'.
<ActionLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.M()' in state 'Init' invoked action 'InitOnEntry'.
<CreateLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.N()' was created by machine 'Microsoft.PSharp.Core.Tests.LogMessages.M()'.
<StateLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.N()' enters state 'Init'.
<SendLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.M()' in state 'Init' sent event 'Microsoft.PSharp.Core.Tests.LogMessages.E' to machine 'Microsoft.PSharp.Core.Tests.LogMessages.N()'.
<EnqueueLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.N()' enqueued event 'Microsoft.PSharp.Core.Tests.LogMessages.E'.
<DequeueLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.N()' in state 'Init' dequeued event 'Microsoft.PSharp.Core.Tests.LogMessages.E'.
<ActionLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.N()' in state 'Init' invoked action 'Act'.
<SendLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.N()' in state 'Init' sent event 'Microsoft.PSharp.Core.Tests.LogMessages.E' to machine 'Microsoft.PSharp.Core.Tests.LogMessages.M()'.
<EnqueueLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.M()' enqueued event 'Microsoft.PSharp.Core.Tests.LogMessages.E'.
<DequeueLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.M()' in state 'Init' dequeued event 'Microsoft.PSharp.Core.Tests.LogMessages.E'.
<ActionLog> Machine 'Microsoft.PSharp.Core.Tests.LogMessages.M()' in state 'Init' invoked action 'Act'.
";
            string actual = Regex.Replace(logger.ToString(), "[0-9]", string.Empty);

            HashSet<string> expectedSet = new HashSet<string>(Regex.Split(expected, "\r\n|\r|\n"));
            HashSet<string> actualSet = new HashSet<string>(Regex.Split(actual, "\r\n|\r|\n"));

            Assert.True(expectedSet.SetEquals(actualSet));

            logger.Dispose();
        }

        [Fact(Timeout=5000)]
        public async Task TestCustomLoggerNoVerbosity()
        {
            CustomLogger logger = new CustomLogger(false);

            var runtime = PSharpRuntime.Create();
            runtime.SetLogger(logger);

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));

            await WaitAsync(tcs.Task);

            Assert.Equal(string.Empty, logger.ToString());

            logger.Dispose();
        }

        [Fact(Timeout=5000)]
        public void TestNullCustomLoggerFail()
        {
            this.Run(r =>
            {
                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => r.SetLogger(null));
                Assert.Equal("Cannot install a null logger.", ex.Message);
            });
        }
    }
}
