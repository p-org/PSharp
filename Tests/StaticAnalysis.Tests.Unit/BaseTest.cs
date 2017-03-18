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
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis.Tests.Unit
{
    public abstract class BaseTest
    {
        protected Configuration GetConfiguration()
        {
            var configuration = Configuration.Create();
            configuration.ProjectName = "Test";
            configuration.ThrowInternalExceptions = true;
            configuration.Verbose = 2;
            configuration.AnalyzeDataFlow = true;
            configuration.AnalyzeDataRaces = true;
            return configuration;
        }

        private CompilationContext RunTest(Configuration configuration, string test, bool isPSharpProgram)
        {
            Output.SetLogger(new InMemoryLogger());

            var context = CompilationContext.Create(configuration).LoadSolution(test, isPSharpProgram ? "psharp" : "cs");

            ParsingEngine.Create(context).Run();
            RewritingEngine.Create(context).Run();

            AnalysisErrorReporter.ResetStats();
            StaticAnalysisEngine.Create(context).Run();
            return context;
        }

        #region successful tests

        private void TestComplete()
        {
            Output.RemoveLogger();
        }

        protected void AssertSucceeded(string test, bool isPSharpProgram = true)
        {
            var configuration = GetConfiguration();
            AssertSucceeded(configuration, test, isPSharpProgram);
        }

        protected void AssertSucceeded(Configuration configuration, string test, bool isPSharpProgram = true)
        { 
            try
            {
                var context = RunTest(configuration, test, isPSharpProgram);
                var stats = AnalysisErrorReporter.GetStats();
                var expected = "No static analysis errors detected.";
                Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);
            }
            finally
            {
                TestComplete();
            }
        }

        #endregion

        #region failed tests

        protected void AssertFailed(string test, int numExpectedErrors, bool isPSharpProgram = true)
        {
            var configuration = GetConfiguration();
            AssertFailed(configuration, test, numExpectedErrors, isPSharpProgram);
        }

        protected void AssertFailed(string test, int numExpectedErrors, string expectedOutput, bool isPSharpProgram = true)
        {
            var configuration = GetConfiguration();
            AssertFailed(configuration, test, numExpectedErrors, expectedOutput, isPSharpProgram);
        }

        protected void AssertFailed(Configuration configuration, string test, int numExpectedErrors, bool isPSharpProgram = true)
        {
            AssertFailed(configuration, test, numExpectedErrors, string.Empty, isPSharpProgram);
        }

        protected void AssertFailed(Configuration configuration, string test, int numExpectedErrors, string expectedOutput, bool isPSharpProgram = true)
        {
            try
            {
                var context = RunTest(configuration, test, isPSharpProgram);

                var stats = AnalysisErrorReporter.GetStats();
                var errorSuffix = numExpectedErrors > 1 ? "s" : string.Empty;
                var expected = $"Static analysis detected '{numExpectedErrors}' error{errorSuffix}.";
                Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

                if (!string.IsNullOrEmpty(expectedOutput))
                {
                    var actual = ((InMemoryLogger)Output.Logger).ToString();
                    Assert.AreEqual(expectedOutput.Replace(Environment.NewLine, string.Empty),
                       actual.Substring(0, actual.IndexOf(Environment.NewLine)));
                }
            }
            finally
            {
                TestComplete();
            }
        }

        #endregion

        #region warning tests

        protected void AssertWarning(string test, int numExpectedWarnings, bool isPSharpProgram = true)
        {
            var configuration = GetConfiguration();
            AssertWarning(configuration, test, numExpectedWarnings, isPSharpProgram);
        }

        protected void AssertWarning(string test, int numExpectedWarnings, string expectedOutput, bool isPSharpProgram = true)
        {
            var configuration = GetConfiguration();
            AssertWarning(configuration, test, numExpectedWarnings, expectedOutput, isPSharpProgram);
        }

        protected void AssertWarning(Configuration configuration, string test, int numExpectedWarnings, bool isPSharpProgram = true)
        {
            AssertWarning(configuration, test, numExpectedWarnings, string.Empty, isPSharpProgram);
        }

        protected void AssertWarning(Configuration configuration, string test, int numExpectedWarnings, string expectedOutput, bool isPSharpProgram = true)
        {
            AssertFailedAndWarning(configuration, test, 0, numExpectedWarnings, expectedOutput, isPSharpProgram);
        }

        #endregion

        #region failed with warning tests

        protected void AssertFailedAndWarning(string test, int numExpectedErrors, int numExpectedWarnings, bool isPSharpProgram = true)
        {
            var configuration = GetConfiguration();
            AssertFailedAndWarning(configuration, test, numExpectedErrors, numExpectedWarnings, isPSharpProgram);
        }

        protected void AssertFailedAndWarning(string test, int numExpectedErrors, int numExpectedWarnings, string expectedOutput, bool isPSharpProgram = true)
        {
            var configuration = GetConfiguration();
            AssertFailedAndWarning(configuration, test, numExpectedErrors, numExpectedWarnings, expectedOutput, isPSharpProgram);
        }

        protected void AssertFailedAndWarning(Configuration configuration, string test, int numExpectedErrors, int numExpectedWarnings, bool isPSharpProgram = true)
        {
            AssertFailedAndWarning(configuration, test, numExpectedErrors, numExpectedWarnings, string.Empty, isPSharpProgram);
        }

        protected void AssertFailedAndWarning(Configuration configuration, string test, int numExpectedErrors, int numExpectedWarnings, string expectedOutput, bool isPSharpProgram = true)
        {
            try
            {
                ErrorReporter.ShowWarnings = true;
                var context = RunTest(configuration, test, isPSharpProgram);

                var stats = AnalysisErrorReporter.GetStats();
                var errorSuffix = numExpectedErrors == 1 ? string.Empty : "s";
                var warningSuffix = numExpectedWarnings == 1 ? string.Empty : "s";
                var expected = $"Static analysis detected '{numExpectedErrors}' error{errorSuffix} and '{numExpectedWarnings}' warning{warningSuffix}.";
                Assert.AreEqual(expected.Replace(Environment.NewLine, string.Empty), stats);

                if (!string.IsNullOrEmpty(expectedOutput))
                {
                    var actual = ((InMemoryLogger)Output.Logger).ToString();
                    Assert.AreEqual(expectedOutput.Replace(Environment.NewLine, string.Empty),
                       //actual.Substring(0, actual.IndexOf(Environment.NewLine)));
                       actual.Replace(Environment.NewLine, string.Empty));
                }
            }
            finally
            {
                ErrorReporter.ShowWarnings = false;
                TestComplete();
            }
        }

        #endregion
    }
}
