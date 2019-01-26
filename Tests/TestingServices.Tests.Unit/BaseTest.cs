//-----------------------------------------------------------------------
// <copyright file="BaseTest.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
    public abstract class BaseTest
    {
        public const string NamespaceName = "Microsoft.PSharp.TestingServices.Tests.Unit";

        protected readonly ITestOutputHelper TestOutput;

        public BaseTest(ITestOutputHelper output)
        {
            this.TestOutput = output;
        }

        #region successful tests

        protected void AssertSucceeded(Action<IMachineRuntime> test)
        {
            var configuration = GetConfiguration();
            this.AssertSucceeded(configuration, test);
        }

        protected void AssertSucceeded(Configuration configuration, Action<IMachineRuntime> test)
        {
            TestOutputLogger logger = new TestOutputLogger(this.TestOutput);

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

        protected void AssertFailed(Action<IMachineRuntime> test, int numExpectedErrors, bool replay)
        {
            var configuration = this.GetConfiguration();
            this.AssertFailed(configuration, test, numExpectedErrors, replay);
        }

        protected void AssertFailed(Action<IMachineRuntime> test, string expectedOutput, bool replay)
        {
            var configuration = this.GetConfiguration();
            this.AssertFailed(configuration, test, 1, new HashSet<string> { expectedOutput }, replay);
        }

        protected void AssertFailed(Action<IMachineRuntime> test, int numExpectedErrors, ISet<string> expectedOutputs, bool replay)
        {
            var configuration = this.GetConfiguration();
            this.AssertFailed(configuration, test, numExpectedErrors, expectedOutputs, replay);
        }

        protected void AssertFailed(Configuration configuration, Action<IMachineRuntime> test, int numExpectedErrors, bool replay)
        {
            this.AssertFailed(configuration, test, numExpectedErrors, new HashSet<string>(), replay);
        }

        protected void AssertFailed(Configuration configuration, Action<IMachineRuntime> test, string expectedOutput, bool replay)
        {
            this.AssertFailed(configuration, test, 1, new HashSet<string> { expectedOutput }, replay);
        }

        protected void AssertFailed(Configuration configuration, Action<IMachineRuntime> test, int numExpectedErrors,
            ISet<string> expectedOutputs, bool replay)
        {
            this.AssertFailed(configuration, test, numExpectedErrors, bugReports =>
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

        protected void AssertFailed(Configuration configuration, Action<IMachineRuntime> test, int numExpectedErrors,
            Func<HashSet<string>, bool> expectedOutputFunc, bool replay)
        {
            TestOutputLogger logger = new TestOutputLogger(this.TestOutput);

            try
            {
                var bfEngine = BugFindingEngine.Create(configuration, test);
                bfEngine.SetLogger(logger);
                bfEngine.Run();

                this.CheckErrors(bfEngine, numExpectedErrors, expectedOutputFunc);

                if (replay && !configuration.EnableCycleDetection)
                {
                    var rEngine = ReplayEngine.Create(configuration, test, bfEngine.ReproducableTrace);
                    rEngine.SetLogger(logger);
                    rEngine.Run();

                    Assert.True(rEngine.InternalError.Length == 0, rEngine.InternalError);
                    this.CheckErrors(rEngine, numExpectedErrors, expectedOutputFunc);
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
                //this.TestOutput.WriteLine("actual: " + actual);
                bugReports.Add(actual);
            }

            Assert.True(expectedOutputFunc(bugReports));
        }

        #endregion

        #region tests that throw an exception

        protected void AssertFailedWithException(Action<IMachineRuntime> test, Type exceptionType, bool replay)
        {
            var configuration = this.GetConfiguration();
            this.AssertFailedWithException(configuration, test, exceptionType, replay);
        }

        protected void AssertFailedWithException(Configuration configuration, Action<IMachineRuntime> test, Type exceptionType, bool replay)
        {
            Assert.True(exceptionType.IsSubclassOf(typeof(Exception)), "Please configure the test correctly. " +
                $"Type '{exceptionType}' is not an exception type.");

            TestOutputLogger logger = new TestOutputLogger(this.TestOutput);

            try
            {
                var bfEngine = BugFindingEngine.Create(configuration, test);
                bfEngine.SetLogger(logger);
                bfEngine.Run();

                this.CheckErrors(bfEngine, exceptionType);

                if (replay && !configuration.EnableCycleDetection)
                {
                    var rEngine = ReplayEngine.Create(configuration, test, bfEngine.ReproducableTrace);
                    rEngine.SetLogger(logger);
                    rEngine.Run();

                    Assert.True(rEngine.InternalError.Length == 0, rEngine.InternalError);
                    this.CheckErrors(rEngine, exceptionType);
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
            string report = String.Empty;
            foreach (var bug in engine.TestReport.BugReports)
            {
                report += bug + "\n";
            }

            return report;
        }

        private string RemoveNonDeterministicValuesFromReport(string report)
        {
            var result = Regex.Replace(report, @"\'[0-9]+\'", "''");
            result = Regex.Replace(result, @"\([0-9].?[0-9]?\)", "()");
            result = Regex.Replace(result, @"\[\[.*\]\]", "[[]]");
            this.TestOutput.WriteLine("res: " + result);
            return result;
        }

        #endregion
    }
}
