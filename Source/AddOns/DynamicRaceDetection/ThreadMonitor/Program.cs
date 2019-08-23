// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Utilities.Safe;

using Microsoft.PSharp.Utilities;
using System.IO;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Monitoring
{
    /// <summary>
    /// The P# dynamic data race detector.
    /// </summary>
    public static class Program
    {
        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            // Parses the command line options to get the configuration.
            var configuration = new ThreadMonitorCommandLineOptions(args).Parse();
            // Creates and starts a thread monitoring process.
            ThreadMonitorProcess.Create(configuration).Start();
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
