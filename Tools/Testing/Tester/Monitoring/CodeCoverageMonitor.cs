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
        private static bool IsRunning;

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

            string VSPerfCmdToolPath = GetToolPath();
            var monitorProc = new Process();

            try
            {
                monitorProc.StartInfo.FileName = VSPerfCmdToolPath;
                monitorProc.StartInfo.Arguments = $"/start:coverage /output:{GetOutputName()}";
                monitorProc.StartInfo.UseShellExecute = false;
                monitorProc.StartInfo.RedirectStandardOutput = true;
                monitorProc.StartInfo.RedirectStandardError = true;
                monitorProc.Start();
                monitorProc.WaitForExit();
            }
            finally
            {
                monitorProc.Close();
            }

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

            string VSPerfCmdToolPath = GetToolPath();
            var monitorProc = new Process();

            monitorProc.StartInfo.FileName = VSPerfCmdToolPath;
            monitorProc.StartInfo.Arguments = "/shutdown";
            monitorProc.StartInfo.UseShellExecute = false;
            monitorProc.StartInfo.RedirectStandardOutput = true;
            monitorProc.StartInfo.RedirectStandardError = true;
            monitorProc.Start();

            IsRunning = false;
        }

        /// <summary>
        /// Returns the output name.
        /// </summary>
        /// <returns>Name</returns>
        private static string GetOutputName()
        {
            string file = Path.GetFileNameWithoutExtension(Configuration.AssemblyToBeAnalyzed);
            string directory = Reporter.GetOutputDirectory(Configuration.OutputFilePath, Configuration.AssemblyToBeAnalyzed);

            string[] outputs = Directory.GetFiles(directory, file + "_*.coverage").
                Where(path => new Regex(@"^.*_[0-9]+.coverage").IsMatch(path)).ToArray();
            string output = directory + file + "_" + outputs.Length + ".coverage";

            return output;
        }

        /// <summary>
        /// Returns the tool path to the code coverage monitor.
        /// </summary>
        /// <returns>Tool path</returns>
        private static string GetToolPath()
        {
            string tool = "";
            try
            {
                tool = ConfigurationManager.AppSettings["VSPerfCmdToolPath"];
            }
            catch (ConfigurationErrorsException)
            {
                Error.ReportAndExit("[PSharpTester] required 'VSPerfCmdToolPath' value is not set in configuration file.");
            }

            if (!File.Exists(tool))
            {
                Error.ReportAndExit($"[PSharpTester] 'VSPerfCmd' tool '{tool}' not found.");
            }

            return tool;
        }
    }
}