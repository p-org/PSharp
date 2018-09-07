// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.PSharp.IO;

using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public abstract class BaseTest
    {
        #region successful tests

        protected void AssertSucceeded(Action<PSharpRuntime> test)
        {
            var configuration = GetConfiguration();
            AssertSucceeded(configuration, test);
        }

        protected void AssertSucceeded(Configuration configuration, Action<PSharpRuntime> test)
        {
            InMemoryLogger logger = new InMemoryLogger();

            try
            {
                var engine = BugFindingEngine.Create(configuration, test);
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
        }

        #endregion

        #region tests that fail an assertion

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

        protected void AssertFailed(Configuration configuration, Action<PSharpRuntime> test, int numExpectedErrors, ISet<string> expectedOutputs, bool replay)
        {
            AssertFailed(configuration, test, numExpectedErrors, bugReports =>
            {
                foreach (var expected in expectedOutputs)
                {
                    if (!bugReports.Contains(expected))
                    {
                        return false;
                    }
                }
                return true;
            }, replay);
        }

        protected void AssertFailed(Configuration configuration, Action<PSharpRuntime> test, int numExpectedErrors, Func<HashSet<string>, bool> expectedOutputFunc, bool replay)
        {
            InMemoryLogger logger = new InMemoryLogger();

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

        private void CheckErrors(ITestingEngine engine, int numExpectedErrors, Func<HashSet<string>, bool> expectedOutputFunc)
        {
            var numErrors = engine.TestReport.NumOfFoundBugs;
            Assert.Equal(numExpectedErrors, numErrors);

            var bugReports = new HashSet<string>();
            foreach (var bugReport in engine.TestReport.BugReports)
            {
                var actual = this.RemoveNonDeterministicValuesFromReport(bugReport);
                bugReports.Add(actual);
            }

            Assert.True(expectedOutputFunc(bugReports));
        }

        #endregion

        #region tests that throw an exception

        protected void AssertFailedWithException(Action<PSharpRuntime> test, Type exceptionType, bool replay)
        {
            var configuration = GetConfiguration();
            AssertFailedWithException(configuration, test, exceptionType, replay);
        }

        protected void AssertFailedWithException(Configuration configuration, Action<PSharpRuntime> test, Type exceptionType, bool replay)
        {
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            InMemoryLogger logger = new InMemoryLogger();

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

        private void CheckErrors(ITestingEngine engine, Type exceptionType)
        {
            var numErrors = engine.TestReport.NumOfFoundBugs;
            Assert.Equal(1, numErrors);

            var exception = this.RemoveNonDeterministicValuesFromReport(engine.TestReport.BugReports.First()).
                Split(new[] { '\r', '\n' }).FirstOrDefault();
            Assert.Contains("'" + exceptionType.ToString() + "'", exception);
        }

        #endregion

        #region utilities

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
            result = Regex.Replace(result, @"\([0-9]+\)", "()");
            return result;
        }

        #endregion
    }
}
