// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;
using System;
using System.IO;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# dynamic data race detector.
    /// </summary>
    public static class Program
    {
        private static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            // Parses the command line options to get the configuration.
            var configuration = new RaceDetectorCommandLineOptions(args).Parse();

            string directoryPath = Path.GetDirectoryName(configuration.AssemblyToBeAnalyzed) +
                Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar + "RuntimeTraces";
            if (Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);
            directoryPath = Path.GetDirectoryName(configuration.AssemblyToBeAnalyzed) +
                Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar + "ThreadTraces";
            if (Directory.Exists(directoryPath))
                Directory.Delete(directoryPath, true);

            // Creates and starts a dynamic race detection process.
            RaceDetectionProcess.Create(configuration).Start(args);
        }

        /// <summary>
        /// Handler for unhandled exceptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Output.WriteLine(ex.Message);
            Output.WriteLine(ex.StackTrace);
            IO.Error.ReportAndExit("internal failure: {0}: {1}, {2}",
                ex.GetType().ToString(), ex.Message, ex.StackTrace);
        }
    }
}