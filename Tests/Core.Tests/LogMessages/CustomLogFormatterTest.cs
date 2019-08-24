// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.Core.Tests.LogMessages
{
    public class CustomLogFormatterTest : BaseTest
    {
        public CustomLogFormatterTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout=5000)]
        public async Task TestCustomFormatter()
        {
            CustomLogger logger = new CustomLogger(true);

            Configuration config = Configuration.Create().WithVerbosityEnabled();
            var runtime = PSharpRuntime.Create(config);
            runtime.SetLogger(logger);
            runtime.SetLogFormatter(new CustomLogFormatter());

            var tcs = new TaskCompletionSource<bool>();
            runtime.CreateMachine(typeof(M), new Configure(tcs));

            await WaitAsync(tcs.Task);

            string expected = @"<CreateLog>.
<StateLog>.
<ActionLog>.
<CreateLog>.
<StateLog>.
<SendLog>.
<EnqueueLog>.
<DequeueLog>.
<ActionLog>.
<SendLog>.
<EnqueueLog>.
<DequeueLog>.
<ActionLog>.
";
            string actual = Regex.Replace(logger.ToString(), "[0-9]", string.Empty);

            HashSet<string> expectedSet = new HashSet<string>(Regex.Split(expected, "\r\n|\r|\n"));
            HashSet<string> actualSet = new HashSet<string>(Regex.Split(actual, "\r\n|\r|\n"));

            Assert.True(expectedSet.SetEquals(actualSet));

            logger.Dispose();
        }
    }
}
