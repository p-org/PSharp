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

using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The P# tester.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            // Parses the command line options to get the configuration.
            var configuration = new TesterCommandLineOptions(args).Parse();

            if (configuration.ParallelBugFindingTasks == 1 ||
                configuration.TestingProcessId < 0)
            {
                IO.PrintLine(". Testing " + configuration.AssemblyToBeAnalyzed);
                if(configuration.TestMethodName != "")
                {
                    IO.PrintLine(". Method {0}", configuration.TestMethodName);
                }
            }
            
            if (configuration.ParallelBugFindingTasks == 1 ||
                configuration.TestingProcessId >= 0)
            {
                // Creates and runs a testing process.
                TestingProcess testingProcess = TestingProcess.Create(configuration);
                testingProcess.Start();
            }
            else
            {
                // Creates and runs the testing process scheduler, if there
                // are more than one user specified parallel tasks.
                TestingProcessScheduler.Create(configuration).Run();
            }

            if (configuration.ParallelBugFindingTasks == 1 ||
                configuration.TestingProcessId < 0)
            {
                IO.PrintLine(". Done");
            }
        }

        /// <summary>
        /// Handler for unhandled exceptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            IO.Debug(ex.Message);
            IO.Debug(ex.StackTrace);
            IO.Error.ReportAndExit("internal failure: {0}: {1}", ex.GetType().ToString(), ex.Message);
        }
    }
}
