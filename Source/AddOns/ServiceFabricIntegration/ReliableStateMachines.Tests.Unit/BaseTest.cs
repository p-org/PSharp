using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PSharp.ReliableServices.Tests.Unit
{
    public class BaseTest
    {
        #region successful tests

        protected void AssertSucceeded(Action<PSharpRuntime> test)
        {
            var configuration = Configuration.Create();
            AssertSucceeded(configuration, test);
        }

        protected void AssertSucceeded(Configuration configuration, Action<PSharpRuntime> test)
        {
            InMemoryLogger logger = new InMemoryLogger();

            try
            {
                var engine = BugFindingEngine.Create(configuration, SetupTestWithStateManager(test));
                engine.SetLogger(logger);
                engine.Run();

                var numErrors = engine.TestReport.NumOfFoundBugs;
                Assert.IsTrue(numErrors == 0, GetBugReport(engine));
            }
            catch (Exception ex)
            {
                Assert.IsFalse(true, ex.Message + "\n" + ex.StackTrace);
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
            var configuration = Configuration.Create();
            AssertFailed(configuration, test, numExpectedErrors, replay);
        }

        protected void AssertFailed(Action<PSharpRuntime> test, string expectedOutput, bool replay)
        {
            var configuration = Configuration.Create();
            AssertFailed(configuration, test, 1, new HashSet<string> { expectedOutput }, replay);
        }

        protected void AssertFailed(Action<PSharpRuntime> test, int numExpectedErrors, ISet<string> expectedOutputs, bool replay)
        {
            var configuration = Configuration.Create();
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
                var bfEngine = BugFindingEngine.Create(configuration, SetupTestWithStateManager(test));
                bfEngine.SetLogger(logger);
                bfEngine.Run();

                CheckErrors(bfEngine, numExpectedErrors, expectedOutputFunc);

                if (replay && !configuration.EnableCycleDetection)
                {
                    var rEngine = ReplayEngine.Create(configuration, test, bfEngine.ReproducableTrace);
                    rEngine.SetLogger(logger);
                    rEngine.Run();

                    Assert.IsTrue(rEngine.InternalError.Length == 0, rEngine.InternalError);
                    CheckErrors(rEngine, numExpectedErrors, expectedOutputFunc);
                }
            }
            catch (Exception ex)
            {
                Assert.IsFalse(true, ex.Message + "\n" + ex.StackTrace);
            }
            finally
            {
                logger.Dispose();
            }
        }

        private void CheckErrors(ITestingEngine engine, int numExpectedErrors, Func<HashSet<string>, bool> expectedOutputFunc)
        {
            var numErrors = engine.TestReport.NumOfFoundBugs;
            Assert.AreEqual(numExpectedErrors, numErrors);

            var bugReports = new HashSet<string>();
            foreach (var bugReport in engine.TestReport.BugReports)
            {
                var actual = this.RemoveNonDeterministicValuesFromReport(bugReport);
                bugReports.Add(actual);
            }

            Assert.IsTrue(expectedOutputFunc(bugReports));
        }

        #endregion

        #region private methods

        private Action<PSharpRuntime> SetupTestWithStateManager(Action<PSharpRuntime> test)
        {
            return new Action<PSharpRuntime>(runtime =>
            {
                var mockStateManager = new StateManagerMock(runtime);
                runtime.AddMachineFactory(new ReliableStateMachineFactory(mockStateManager, true));
                test(runtime);
            });
        }

        private string GetBugReport(ITestingEngine engine)
        {
            string report = "";
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
