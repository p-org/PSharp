using System;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# trace replayer.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            // Parses the command line options to get the configuration.
            var configuration = new ReplayerCommandLineOptions(args).Parse();

            // Creates and starts a replaying process.
            ReplayingProcess.Create(configuration).Start();

            Output.WriteLine(". Done");
        }

        /// <summary>
        /// Handler for unhandled exceptions.
        /// </summary>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Error.Report("[PSharpReplayer] internal failure: {0}: {1}", ex.GetType().ToString(), ex.Message);
            Output.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
