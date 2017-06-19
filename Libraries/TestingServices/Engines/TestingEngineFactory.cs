//-----------------------------------------------------------------------
// <copyright file="TestingEngineFactory.cs">
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
using System.Reflection;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# testing engine factory.
    /// </summary>
    public static class TestingEngineFactory
    {
        #region factory methods

        /// <summary>
        /// Runs a P# tests.
        /// </summary>
        /// <param name="args">Flags to PSharpTester</param>
        /// <param name="action">Test method</param>
        /// <returns>TestReport</returns>
        public static TestReport RunTester(string args, Action<PSharpRuntime> action)
        {
            args += " /test:program"; // dummy argument

            var configuration = new Utilities.TesterCommandLineOptions(
                args.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Parse();

            var parallel = configuration.ParallelBugFindingTasks;

            if (configuration.ParallelBugFindingTasks > 1 &&
               configuration.SchedulingStrategy != Utilities.SchedulingStrategy.Portfolio)
            {
                configuration.SchedulingIterations = configuration.SchedulingIterations *
                    (int)configuration.ParallelBugFindingTasks;
                parallel = 1;
            }
            
            configuration.ParallelBugFindingTasks = 1;

            var report = new TestReport(configuration);

            for (uint i = 0; i < parallel; i++)
            {
                configuration.TestingProcessId = i;

                var logger = new IO.InMemoryLogger();
                var engine = BugFindingEngine.Create(configuration, action);
                engine.SetLogger(logger);
                engine.Run();

                report.Merge(engine.TestReport);
            }

            return report;
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration)
        {
            return BugFindingEngine.Create(configuration);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateBugFindingEngine(
            Configuration configuration, Assembly assembly)
        {
            return BugFindingEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateBugFindingEngine(
            Configuration configuration, Action<PSharpRuntime> action)
        {
            return BugFindingEngine.Create(configuration, action);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateReplayEngine(Configuration configuration)
        {
            return ReplayEngine.Create(configuration);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Assembly assembly)
        {
            return ReplayEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateReplayEngine(Configuration configuration,
            Action<PSharpRuntime> action)
        {
            return ReplayEngine.Create(configuration, action);
        }

        #endregion
    }
}
