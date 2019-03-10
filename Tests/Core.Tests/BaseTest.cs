// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;
using Xunit.Abstractions;

using Common = Microsoft.PSharp.Tests.Common;

namespace Microsoft.PSharp.Core.Tests
{
    public abstract class BaseTest : Common.BaseTest
    {
        public BaseTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected void Run(Configuration configuration, Action<PSharpRuntime> test)
        {
            var logger = new Common.TestOutputLogger(this.TestOutput);

            try
            {
                var runtime = PSharpRuntime.Create(configuration);
                runtime.SetLogger(logger);
                test(runtime);
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        protected Configuration GetConfiguration()
        {
            return Configuration.Create();
        }
    }
}
