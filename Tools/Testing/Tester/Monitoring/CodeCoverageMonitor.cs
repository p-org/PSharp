//-----------------------------------------------------------------------
// <copyright file="CodeCoverageMonitor.cs">
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

using Microsoft.PSharp.IO;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Monitors the program being tested for code coverage.
    /// </summary>
    internal static class CodeCoverageMonitor
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private static Configuration Configuration;

        /// <summary>
        /// Monitoring process is running.
        /// </summary>
        internal static bool IsRunning;

        /// <summary>
        /// Starts the code coverage monitor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal static void Start(Configuration configuration)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Process has already started.");
            }

            Configuration = configuration;
            RunMonitorProcess(true);
            IsRunning = true;
        }

        /// <summary>
        /// Stops the code coverage monitor.
        /// </summary>
        internal static void Stop()
        {
            if (Configuration == null)
            {
                throw new InvalidOperationException("Process has not been configured.");
            }

            if (!IsRunning)
            {
                throw new InvalidOperationException("Process is not running.");
            }

            RunMonitorProcess(false);
            IsRunning = false;
        }

        private static void RunMonitorProcess(bool isStarting)
        {
            using (var monitorProc = new Process())
            {
                monitorProc.StartInfo.FileName = CodeCoverageInstrumentation.GetToolPath("VSPerfCmdToolPath", "VSPerfCmd");
                monitorProc.StartInfo.Arguments = isStarting ? $"/start:coverage /output:{GetOutputName()}" : "/shutdown";
                monitorProc.StartInfo.UseShellExecute = false;
                monitorProc.StartInfo.RedirectStandardOutput = true;
                monitorProc.StartInfo.RedirectStandardError = true;
                monitorProc.Start();
                if (isStarting)
                {
                    monitorProc.WaitForExit();
                }
            }
        }

        /// <summary>
        /// Returns the output name.
        /// </summary>
        /// <returns>Name</returns>
        private static string GetOutputName()
        {
            string file = Path.GetFileNameWithoutExtension(Configuration.AssemblyToBeAnalyzed);
            string directory = CodeCoverageInstrumentation.OutputDirectory;
            return $"{directory}{file}.coverage";
        }
    }
}