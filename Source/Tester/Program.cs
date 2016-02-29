//-----------------------------------------------------------------------
// <copyright file="Program.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Utilities.Safe;
using Microsoft.PSharp.Utilities;
using System.IO;
using System.Collections.Generic;
using OfflineRaceDetection;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# language compiler.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(Program.UnhandledExceptionHandler);

            // Parses the command line options to get the configuration.
            var configuration = new TesterCommandLineOptions(args).Parse();

            if (configuration.EnableMonitorableTestingProcess)
            {
                // Creates and starts a monitorable testing process.
                Program.StartMonitorableTestingProcess(configuration, args);

                return;
            }
            else
            {
                // Creates and starts a testing process.
                TestingProcess.Create(configuration).Start();
            }
            IO.PrintLine(". Done");
        }

        /// <summary>
        /// Starts a monitorable testing process.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="args">Arguments</param>
        private static void StartMonitorableTestingProcess(Configuration configuration, string[] args)
        {
            StringCollection referencedAssemblies = new StringCollection();
            String input = configuration.AssemblyToBeAnalyzed;

            Assembly assembly = Assembly.LoadFrom(input);
            referencedAssemblies.Add(assembly.GetName().Name);

            AssemblyName[] assemblyName = assembly.GetReferencedAssemblies();
            foreach (AssemblyName item in assemblyName)
            {
                if (item.Name.Contains("mscorlib") || item.Name.Contains("System") ||
                    item.Name.Contains("NLog") || item.Name.Contains("System.Core"))
                {
                    continue;
                }

                referencedAssemblies.Add(item.Name);
            }

            string[] includedAssemblies = new string[referencedAssemblies.Count];
            referencedAssemblies.CopyTo(includedAssemblies, 0);

            var newArgs = args.ToList();
            newArgs.Remove("/race-detection");
            newArgs.Add("/race-detection-no-monitorable-process");

            configuration.DirectoryPath = "..\\..\\Binaries\\Debug\\";
            IEnumerable<string> dirNames = Directory.EnumerateDirectories(configuration.DirectoryPath);
            foreach(string item in dirNames)
            {
                if (item.Contains("InstrTrace"))
                {
                    Directory.Delete(configuration.DirectoryPath + item, true);
                }
            }

            ProcessStartInfo info = ControllerSetUp.GetMonitorableProcessStartInfo(
                "..\\..\\Binaries\\Debug\\Microsoft.PSharp.DynamicRaceDetection.exe", // filename
                new String[] { WrapString(input), configuration.MainClass, configuration.DirectoryPath }, // arguments
                MonitorInstrumentationFlags.All, // monitor flags
                true, // track gc accesses

                null, // we don't monitor process at startup since it loads the DLL to monitor
                null, // user type

                null, // substitution assemblies
                null, // types to monitor
                null, // types to exclude monitor
                null, // namespaces to monitor
                null, // namespaces to exclude monitor
                includedAssemblies,
                null, //assembliesToExcludeMonitor to exclude monitor

                null,
                null, null, null,
                null, null,

                null, // clrmonitor log file name
                false, // clrmonitor  log verbose
                null, // crash on failure
                true, // protect all cctors
                false, // disable mscrolib suppressions
                ProfilerInteraction.Fail, // profiler interaction
                null, "", ""
                );
            IO.PrintLine(". Starts monitorable testing process");

            var process = new Process();
            process.StartInfo = info;
            process.Start();
            process.WaitForExit();

            IO.PrintLine("Done monitoring process");
            Console.ReadLine();
            OfflineRaceDetection.Program.findRaces();
        }

        /// <summary>
        /// Wraps the given string.
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns></returns>
        private static string WrapString(string text)
        {
            if (text == null)
            {
                return text;
            }
            else
            {
                return SafeString.IndexOf(text, ' ') != -1 ? "\"" + text.TrimEnd('\\') + "\"" : text;
            }  
        }

        /// <summary>
        /// Handler for unhandled exceptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            IO.Debug(ex.Message);
            IO.Debug(ex.StackTrace);
            ErrorReporter.ReportAndExit("internal failure: {0}: {1}", ex.GetType().ToString(), ex.Message);
        }
    }
}
