﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using Microsoft.PSharp.IO;
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

        protected void Run(Configuration configuration, Action<IMachineRuntime> test)
        {
            var logger = new ThreadSafeInMemoryLogger();
            var outputLogger = new Common.TestOutputLogger(this.TestOutput);

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
                this.TestOutput.WriteLine(logger.ToString());
                logger.Dispose();
                outputLogger.Dispose();
            }
        }

        protected static Configuration GetConfiguration()
        {
            return Configuration.Create();
        }
    }
}
