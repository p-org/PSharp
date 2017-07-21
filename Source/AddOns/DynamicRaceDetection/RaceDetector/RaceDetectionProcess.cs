//-----------------------------------------------------------------------
// <copyright file="RaceDetectionProcess.cs">
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

using Microsoft.ExtendedReflection.Monitoring;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.PSharp
{
    /// <summary>
    /// A P# dynamic race detection process.
    /// </summary>
    internal sealed class RaceDetectionProcess
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private Configuration Configuration;

        #endregion fields

        #region API

        /// <summary>
        /// Creates a P# dynamic race detection process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>RaceDetectionProcess</returns>
        public static RaceDetectionProcess Create(Configuration configuration)
        {
            return new RaceDetectionProcess(configuration);
        }

        /// <summary>
        /// Starts the P# dynamic race detection process.
        /// </summary>
        /// <param name="args">Arguments</param>
        public void Start(string[] args)
        {
            StringCollection referencedAssemblies = new StringCollection();
            StringCollection excludedAssemblies = new StringCollection();
            string input = this.Configuration.AssemblyToBeAnalyzed;

            Assembly assembly = Assembly.LoadFrom(input);
            referencedAssemblies.Add(assembly.GetName().Name);

            AssemblyName[] assemblyName = assembly.GetReferencedAssemblies();
            foreach (AssemblyName item in assemblyName)
            {
                if (item.Name.Contains("mscorlib") || item.Name.Contains("System") || item.Name.Contains("Microsoft.PSharp") ||
                    item.Name.Contains("NLog") || item.Name.Contains("System.Core"))
                {
                    Console.WriteLine("Excluding " + item.Name);
                    excludedAssemblies.Add(item.Name);
                    continue;
                }
                Console.WriteLine("Including " + item.Name);
                referencedAssemblies.Add(item.Name);
            }

            string[] includedAssemblies = new string[referencedAssemblies.Count];
            string[] excluded = new string[excludedAssemblies.Count];
            referencedAssemblies.CopyTo(includedAssemblies, 0);
            excludedAssemblies.CopyTo(excluded, 0);
            string[] excludedNamespaces = new string[] { "Microsoft.PSharp", "Microsoft.PSharp.Monitors", "Microsoft.PSharp.IO" };

            ProcessStartInfo info = ControllerSetUp.GetMonitorableProcessStartInfo(
                AppDomain.CurrentDomain.BaseDirectory + "\\ThreadMonitor.exe",
                args, // arguments
                MonitorInstrumentationFlags.All, // monitor flags
                true, // track gc accesses

                null, // we don't monitor process at startup since it loads the DLL to monitor
                null, // user type

                null, // substitution assemblies
                null, // types to monitor
                null, // types to exclude monitor
                null, // namespaces to monitor
                excludedNamespaces, // namespaces to exclude monitor
                includedAssemblies, // assemblies to monitor
                excluded, //assemblies to exclude monitor

                null, null, null, null, null, null,

                null, // clrmonitor log file name
                false, // clrmonitor  log verbose
                null, // crash on failure
                true, // protect all cctors
                false, // disable mscrolib suppressions
                ProfilerInteraction.Fail, // profiler interaction
                null, "", "");

            var process = new Process();
            process.StartInfo = info;
            process.Start();
            process.WaitForExit();
        }

        #endregion API

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private RaceDetectionProcess(Configuration configuration)
        {
            this.Configuration = configuration;
        }

        #endregion private methods
    }
}