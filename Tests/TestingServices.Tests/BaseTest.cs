﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

using Common = Microsoft.PSharp.Tests.Common;

namespace Microsoft.PSharp.TestingServices.Tests
{
    public abstract class BaseTest : Common.BaseTest
    {
        public BaseTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected void AssertSucceeded(Action<PSharpRuntime> test)
        {
            var configuration = GetConfiguration();
            AssertSucceeded(configuration, test);
        }

        protected ITestingEngine AssertSucceeded(Configuration configuration, Action<PSharpRuntime> test)
        {
            BugFindingEngine engine = null;
            var logger = new Common.TestOutputLogger(this.TestOutput);

            try
            {
                engine = BugFindingEngine.Create(configuration, test);
                engine.SetLogger(logger);
                engine.Run();

                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.True(numErrors == 0, GetBugReport(engine));
            }
            catch (Exception ex)
            {
                Assert.False(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }

            return engine;
        }

        protected void AssertFailed(Action<PSharpRuntime> test, int numExpectedErrors, bool replay)
        {
            var configuration = GetConfiguration();
            AssertFailed(configuration, test, numExpectedErrors, replay);
        }

        protected void AssertFailed(Action<PSharpRuntime> test, string expectedOutput, bool replay)
        {
            var configuration = GetConfiguration();
            AssertFailed(configuration, test, 1, new HashSet<string> { expectedOutput }, replay);
        }

        protected void AssertFailed(Action<PSharpRuntime> test, int numExpectedErrors, ISet<string> expectedOutputs, bool replay)
        {
            var configuration = GetConfiguration();
            AssertFailed(configuration, test, numExpectedErrors, expectedOutputs, replay);
        }

        protected void AssertFailed(Configuration configuration, Action<PSharpRuntime> test, int numExpectedErrors, bool replay)
        {
            AssertFailed(configuration, test, numExpectedErrors, new HashSet<string>(), replay);
        }

        protected void AssertFailed(Configuration configuration, Action<PSharpRuntime> test, string expectedOutput, bool replay)
        {
            AssertFailed(configuration, test, 1, new HashSet<string> { expectedOutput }, replay);
        }

        protected void AssertFailed(Configuration configuration, Action<PSharpRuntime> test, int numExpectedErrors,
            ISet<string> expectedOutputs, bool replay)
        {
            AssertFailed(configuration, test, numExpectedErrors, bugReports =>
            {
                foreach (var expected in expectedOutputs)
                {
                    this.TestOutput.WriteLine("expected: " + expected);
                    if (!bugReports.Contains(expected))
                    {
                        return false;
                    }
                }
                return true;
            }, replay);
        }

        protected void AssertFailed(Configuration configuration, Action<PSharpRuntime> test, int numExpectedErrors,
            Func<HashSet<string>, bool> expectedOutputFunc, bool replay)
        {
            var logger = new Common.TestOutputLogger(this.TestOutput);

            try
            {
                var bfEngine = BugFindingEngine.Create(configuration, test);
                bfEngine.SetLogger(logger);
                bfEngine.Run();

                CheckErrors(bfEngine, numExpectedErrors, expectedOutputFunc);

                if (replay && !configuration.EnableCycleDetection)
                {
                    var rEngine = ReplayEngine.Create(configuration, test, bfEngine.ReproducableTrace);
                    rEngine.SetLogger(logger);
                    rEngine.Run();

                    Assert.True(rEngine.InternalError.Length == 0, rEngine.InternalError);
                    CheckErrors(rEngine, numExpectedErrors, expectedOutputFunc);
                }
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

        protected void AssertFailedWithException(Action<PSharpRuntime> test, Type exceptionType, bool replay)
        {
            var configuration = GetConfiguration();
            AssertFailedWithException(configuration, test, exceptionType, replay);
        }

        protected void AssertFailedWithException(Configuration configuration, Action<PSharpRuntime> test, Type exceptionType, bool replay)
        {
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            var logger = new Common.TestOutputLogger(this.TestOutput);

            try
            {
                var bfEngine = BugFindingEngine.Create(configuration, test);
                bfEngine.SetLogger(logger);
                bfEngine.Run();

                CheckErrors(bfEngine, exceptionType);

                if (replay && !configuration.EnableCycleDetection)
                {
                    var rEngine = ReplayEngine.Create(configuration, test, bfEngine.ReproducableTrace);
                    rEngine.SetLogger(logger);
                    rEngine.Run();

                    Assert.True(rEngine.InternalError.Length == 0, rEngine.InternalError);
                    CheckErrors(rEngine, exceptionType);
                }
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

        private void CheckErrors(ITestingEngine engine, int numExpectedErrors, Func<HashSet<string>, bool> expectedOutputFunc)
        {
            var numErrors = engine.TestReport.NumOfFoundBugs;
            Assert.Equal(numExpectedErrors, numErrors);

            var bugReports = new HashSet<string>();
            foreach (var bugReport in engine.TestReport.BugReports)
            {
                var actual = this.RemoveNonDeterministicValuesFromReport(bugReport);
                this.TestOutput.WriteLine("actual: " + actual);
                bugReports.Add(actual);
            }

            Assert.True(expectedOutputFunc(bugReports));
        }

        private void CheckErrors(ITestingEngine engine, Type exceptionType)
        {
            var numErrors = engine.TestReport.NumOfFoundBugs;
            Assert.Equal(1, numErrors);

            var exception = this.RemoveNonDeterministicValuesFromReport(engine.TestReport.BugReports.First()).
                Split(new[] { '\r', '\n' }).FirstOrDefault();
            Assert.Contains("'" + exceptionType.ToString() + "'", exception);
        }

        protected Configuration GetConfiguration()
        {
            return Configuration.Create();
        }

        private string GetBugReport(ITestingEngine engine)
        {
            string report = string.Empty;
            foreach (var bug in engine.TestReport.BugReports)
            {
                report += bug + "\n";
            }

            return report;
        }

        private string RemoveNonDeterministicValuesFromReport(string report)
        {
            var result = Regex.Replace(report, @"\'[0-9]+\'", "''");
            result = Regex.Replace(result, @"\([^)]*\)", "()");
            result = Regex.Replace(result, @"\[[^)]*\]", "[]");
            result = Regex.Replace(result, @"Microsoft\.[^+]*\+", "");
            return result;
        }
    }
}
