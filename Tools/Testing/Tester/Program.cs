//-----------------------------------------------------------------------
// <copyright file="Program.cs">
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
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# tester.
    /// </summary>
    class Program
    {
        private static Configuration configuration;
		private static TestingProcessScheduler tpscheduler;

        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            // Parses the command line options to get the configuration.
            configuration = new TesterCommandLineOptions(args).Parse();
            Console.CancelKeyPress += (sender, eventArgs) => CancelProcess();

            if (configuration.RunAsParallelBugFindingTask)
            {
                // Creates and runs a testing process.
                TestingProcess testingProcess = TestingProcess.Create(configuration);
                testingProcess.Start();
                return;
            }

            if (configuration.ReportCodeCoverage || configuration.ReportActivityCoverage)
            {
                // This has to be here because both forms of coverage require it.
                CodeCoverageInstrumentation.SetOutputDirectory(configuration, makeHistory:true);
            }

            if (configuration.ReportCodeCoverage)
            {
                // Instruments the program under test for code coverage.
                CodeCoverageInstrumentation.Instrument(configuration);
                // Starts monitoring for code coverage.
                CodeCoverageMonitor.Start(configuration);
            }

            Output.WriteLine(". Testing " + configuration.AssemblyToBeAnalyzed);
            if (configuration.TestMethodName != "")
            {
                Output.WriteLine("... Method {0}", configuration.TestMethodName);
            }

			// Creates and runs the testing process scheduler.
			tpscheduler = TestingProcessScheduler.Create(configuration);
			tpscheduler.Run();
            Shutdown();

            Output.WriteLine(". Done");
        }

        static void Shutdown()
        {
            if (configuration != null && configuration.ReportCodeCoverage && CodeCoverageMonitor.IsRunning)
            {
                // Stops monitoring for code coverage.
                CodeCoverageMonitor.Stop();
                CodeCoverageInstrumentation.Restore();
            }
        }

        static void CancelProcess()
        {
			if (tpscheduler != null)
				tpscheduler.Stop();

			// Delay shutting down to allow the spawned testing processes to end gracefully.
			Task.Run(async () => {
				await Task.Delay(5000);
			});

			var monitorMessage = CodeCoverageMonitor.IsRunning ? " Shutting down the code coverage monitor (this may take a few seconds)..." : string.Empty;
            Output.WriteLine($". Process canceled by user.{monitorMessage}");
            Shutdown();
        }

        /// <summary>
        /// Handler for unhandled exceptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Error.Report("[PSharpTester] internal failure: {0}: {1}", ex.GetType().ToString(), ex.Message);
            Output.WriteLine(ex.StackTrace);
            Shutdown();
            Environment.Exit(1);
        }
    }
}