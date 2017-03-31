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
using System.Reflection;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.Utilities;

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
            //currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            // Parses the command line options to get the configuration.
            var configuration = new CompilerCommandLineOptions(args).Parse();

            // If the compiler compiles for testing, the optimization should be debug.
            if (configuration.CompilationTarget == CompilationTarget.Testing)
            {
                configuration.OptimizationTarget = OptimizationTarget.Debug;
            }

            // Enables colored console output.
            configuration.EnableColoredConsoleOutput = true;

            // Creates the compilation context and loads the solution.
            var context = CompilationContext.Create(configuration).LoadSolution();

            DefaultLogger logger = new DefaultLogger();
            ErrorReporter errorReporter = new ErrorReporter(configuration, logger);

            try
            {
                // Creates and starts a parsing process.
                ParsingProcess.Create(context).Start();

                // Creates and starts a rewriting process.
                RewritingProcess.Create(context).Start();

                // Creates and starts a static analysis process.
                StaticAnalysisProcess.Create(context).Start();

                // Creates and starts a compilation process.
                CompilationProcess.Create(context, logger).Start();
            }
            catch (ParsingException ex)
            {
                foreach (var warning in ex.Warnings)
                {
                    errorReporter.WriteWarningLine(warning);
                }

                if (ex.Warnings.Count > 0)
                {
                    logger.WriteLine("Found {0} parsing warning{1}.", ex.Warnings.Count,
                        ex.Warnings.Count == 1 ? "" : "s");
                }

                foreach (var error in ex.Errors)
                {
                    errorReporter.WriteErrorLine(error);
                }

                if (ex.Errors.Count > 0)
                {
                    logger.WriteLine("Found {0} parsing error{1}.", ex.Errors.Count,
                        ex.Errors.Count == 1 ? "" : "s");
                }
            }
            catch (RewritingException ex)
            {
                if (ex.InnerException is ReflectionTypeLoadException)
                {
                    var loadException = ex.InnerException as ReflectionTypeLoadException;
                    foreach (var le in loadException.LoaderExceptions)
                    {
                        errorReporter.WriteErrorLine(le.Message);
                    }
                }
                else
                {
                    errorReporter.WriteErrorLine(ex.InnerException.Message);
                }

                Error.ReportAndExit(ex.Message);
            }

            Output.WriteLine(". Done");
        }

        /// <summary>
        /// Handler for unhandled exceptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Error.Report("[PSharpCompiler] internal failure: {0}: {1}", ex.GetType().ToString(), ex.Message);
            Output.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
