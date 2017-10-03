﻿//-----------------------------------------------------------------------
// <copyright file="Program.cs">
//      Copyright (c) 2016 Microsoft Corporation. All rights reserved.
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