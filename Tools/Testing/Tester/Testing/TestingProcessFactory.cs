//-----------------------------------------------------------------------
// <copyright file="TestingProcessFactory.cs">
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

using System.Diagnostics;
using System.Reflection;
using System.Text;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# testing process factory.
    /// </summary>
    internal static class TestingProcessFactory
    {
        #region public methods

        /// <summary>
        /// Creates a new P# testing process.
        /// </summary>
        /// <param name="id">Unique process id</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Process</returns>
        public static Process Create(int id, Configuration configuration)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(
                Assembly.GetExecutingAssembly().Location,
                CreateArgumentsFromConfiguration(id, configuration));
            startInfo.UseShellExecute = false;

            Process process = new Process();
            process.StartInfo = startInfo;

            return process;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Creates arguments from the specified configuration.
        /// </summary>
        /// <param name="id">Unique process id</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>Arguments</returns>
        private static string CreateArgumentsFromConfiguration(int id, Configuration configuration)
        {
            StringBuilder arguments = new StringBuilder();

            if (configuration.EnableDebugging)
            {
                arguments.Append($"/debug ");
            }

            arguments.Append($"/test:{configuration.AssemblyToBeAnalyzed} ");
            if (configuration.TestMethodName != "")
            {
                arguments.Append($"/method:{configuration.TestMethodName} ");
            }

            arguments.Append($"/i:{configuration.SchedulingIterations} ");
            arguments.Append($"/timeout:{configuration.Timeout} ");

            if (configuration.UserExplicitlySetMaxFairSchedulingSteps)
            {
                arguments.Append($"/max-steps:{configuration.MaxUnfairSchedulingSteps}:" +
                    $"{configuration.MaxFairSchedulingSteps} ");
            }
            else
            {
                arguments.Append($"/max-steps:{configuration.MaxUnfairSchedulingSteps} ");
            }

            if (configuration.SchedulingStrategy == SchedulingStrategy.PCT)
            {
                arguments.Append($"/sch:{configuration.SchedulingStrategy}:" +
                    $"{configuration.PrioritySwitchBound} ");
            }
            else if (configuration.SchedulingStrategy == SchedulingStrategy.Random ||
                configuration.SchedulingStrategy == SchedulingStrategy.Portfolio)
            {
                arguments.Append($"/sch:{configuration.SchedulingStrategy} ");
            }

            if (configuration.ReportCodeCoverage)
            {
                arguments.Append($"/coverage-report ");
            }

            arguments.Append($"/parallel:{configuration.ParallelBugFindingTasks} ");
            arguments.Append($"/testing-scheduler-process-id:{Process.GetCurrentProcess().Id} ");
            arguments.Append($"/testing-process-id:{id}");

            return arguments.ToString();
        }

        #endregion
    }
}
