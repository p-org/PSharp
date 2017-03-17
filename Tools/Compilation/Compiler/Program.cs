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

using Microsoft.PSharp.IO;
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

            // Creates the compilation context and loads the solution.
            var context = CompilationContext.Create(configuration).LoadSolution();

            // Creates and starts a parsing process.
            ParsingProcess.Create(context).Start();

            // Creates and starts a rewriting process.
            RewritingProcess.Create(context).Start();

            // Creates and starts a static analysis process.
            StaticAnalysisProcess.Create(context).Start();

            // Creates and starts a compilation process.
            CompilationProcess.Create(context).Start();

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
