//-----------------------------------------------------------------------
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

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Utilities.Safe;

using Microsoft.PSharp.Utilities;

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

            IO.PrintLine(". Done");
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
            IO.Error.ReportAndExit("internal failure: {0}: {1}, {2}",
                ex.GetType().ToString(), ex.Message, ex.StackTrace);
        }
    }
}
