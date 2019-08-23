﻿using System;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# tester.
    /// </summary>
    internal class Program
    {
        private static Configuration configuration;

        private static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            // Parses the command line options to get the configuration.
            configuration = new TesterCommandLineOptions(args).Parse();
            Console.CancelKeyPress += (sender, eventArgs) => CancelProcess();

#if NET46
            if (configuration.RunAsParallelBugFindingTask)
            {
                // Creates and runs a testing process.
                TestingProcess testingProcess = TestingProcess.Create(configuration);
                testingProcess.Run();
                return;
            }

            if (configuration.ReportCodeCoverage || configuration.ReportActivityCoverage)
            {
                // This has to be here because both forms of coverage require it.
                CodeCoverageInstrumentation.SetOutputDirectory(configuration, makeHistory: true);
            }

            if (configuration.ReportCodeCoverage)
            {
                // Instruments the program under test for code coverage.
                CodeCoverageInstrumentation.Instrument(configuration);

                // Starts monitoring for code coverage.
                CodeCoverageMonitor.Start(configuration);
            }
#endif

            Output.WriteLine(". Testing " + configuration.AssemblyToBeAnalyzed);
            if (!string.IsNullOrEmpty(configuration.TestMethodName))
            {
                Output.WriteLine("... Method {0}", configuration.TestMethodName);
            }

            // Creates and runs the testing process scheduler.
            TestingProcessScheduler.Create(configuration).Run();
            Shutdown();

            Output.WriteLine(". Done");
        }

        /// <summary>
        /// Shutdowns any active monitors.
        /// </summary>
        private static void Shutdown()
        {
#if NET46
            if (configuration != null && configuration.ReportCodeCoverage && CodeCoverageMonitor.IsRunning)
            {
                // Stops monitoring for code coverage.
                CodeCoverageMonitor.Stop();
                CodeCoverageInstrumentation.Restore();
            }
#endif
        }

        /// <summary>
        /// Cancels the testing process.
        /// </summary>
        private static void CancelProcess()
        {
            if (TestingProcessScheduler.ProcessCanceled)
            {
                return;
            }

            TestingProcessScheduler.ProcessCanceled = true;

#if NET46
            var monitorMessage = CodeCoverageMonitor.IsRunning ? " Shutting down the code coverage monitor (this may take a few seconds)..." : string.Empty;
#else
            var monitorMessage = string.Empty;
#endif
            Output.WriteLine($". Process canceled by user.{monitorMessage}");
            Shutdown();
        }

        /// <summary>
        /// Handler for unhandled exceptions.
        /// </summary>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Error.Report("[PSharpTester] internal failure: {0}: {1}", ex.GetType().ToString(), ex.Message);
            Output.WriteLine(ex.StackTrace);
            Shutdown();
            Environment.Exit(1);
        }
    }
}
