// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
    internal class Program
    {
        private static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;

            // currentDomain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

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

            ConsoleLogger logger = new ConsoleLogger();
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
                        ex.Warnings.Count == 1 ? string.Empty : "s");
                }

                foreach (var error in ex.Errors)
                {
                    errorReporter.WriteErrorLine(error);
                }

                if (ex.Errors.Count > 0)
                {
                    logger.WriteLine("Found {0} parsing error{1}.", ex.Errors.Count,
                        ex.Errors.Count == 1 ? string.Empty : "s");
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
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var ex = (Exception)args.ExceptionObject;
            Error.Report("[PSharpCompiler] internal failure: {0}: {1}", ex.GetType().ToString(), ex.Message);
            Output.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
