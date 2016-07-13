//-----------------------------------------------------------------------
// <copyright file="CompilerCommandLineOptions.cs">
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

using Microsoft.PSharp.LanguageServices.Compilation;

namespace Microsoft.PSharp.Utilities
{
    public sealed class CompilerCommandLineOptions : BaseCommandLineOptions
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public CompilerCommandLineOptions(string[] args)
            : base (args)
        {
            
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parses the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected override void ParseOption(string option)
        {
            if (option.ToLower().StartsWith("/t:") && option.Length > 3)
            {
                if (option.ToLower().Substring(3).Equals("exe"))
                {
                    base.Configuration.CompilationTarget = CompilationTarget.Execution;
                }
                else if (option.ToLower().Substring(3).Equals("lib"))
                {
                    base.Configuration.CompilationTarget = CompilationTarget.Library;
                }
                else if (option.ToLower().Substring(3).Equals("test"))
                {
                    base.Configuration.CompilationTarget = CompilationTarget.Testing;
                }
                else if (option.ToLower().Substring(3).Equals("remote"))
                {
                    base.Configuration.CompilationTarget = CompilationTarget.Remote;
                }
                else
                {
                    IO.Error.ReportAndExit("Please give a valid compilation target " +
                        "'/t:[x]', where [x] is 'all', 'exe', 'lib' or 'test'.");
                }
            }
            else if (option.ToLower().StartsWith("/optimization:") && option.Length > 14)
            {
                if (option.ToLower().Substring(14).Equals("debug"))
                {
                    base.Configuration.OptimizationTarget = OptimizationTarget.Debug;
                }
                else if (option.ToLower().Substring(14).Equals("release"))
                {
                    base.Configuration.OptimizationTarget = OptimizationTarget.Release;
                }
                else
                {
                    IO.Error.ReportAndExit("Please give a valid optimization target " +
                        "'/optimization:[x]', where [x] is 'debug' or 'release'.");
                }
            }
            else if (option.ToLower().StartsWith("/pass:") && option.Length > 6)
            {
                if (!option.ToLower().Substring(6).EndsWith(".dll"))
                {
                    IO.Error.ReportAndExit("Please give a valid custom compiler pass dll " +
                        "'/pass:[x]', where [x] is a 'dll'.");
                }

                base.Configuration.CustomCompilerPassAssemblyPaths.Add(option.Substring(6));
            }
            else if (option.ToLower().Equals("/dfa"))
            {
                base.Configuration.AnalyzeDataFlow = true;
            }
            else if (option.ToLower().Equals("/check-races"))
            {
                base.Configuration.AnalyzeDataRaces = true;
            }
            else if (option.ToLower().Equals("/emit-control-flow"))
            {
                base.Configuration.ShowControlFlowInformation = true;
            }
            else if (option.ToLower().Equals("/emit-data-flow"))
            {
                base.Configuration.ShowDataFlowInformation = true;
            }
            else if (option.ToLower().StartsWith("/emit-data-flow:") && option.Length > 16)
            {
                if (option.ToLower().Substring(16).Equals("default"))
                {
                    base.Configuration.ShowDataFlowInformation = true;
                }
                else if (option.ToLower().Substring(16).Equals("full"))
                {
                    base.Configuration.ShowFullDataFlowInformation = true;
                }
                else
                {
                    IO.Error.ReportAndExit("Please give a valid data-flow information " +
                        "level '/emit-data-flow:[x]', where [x] is 'default' or 'full'.");
                }
            }
            else if (option.ToLower().Equals("/time"))
            {
                base.Configuration.EnableProfiling = true;
            }
            else if (option.ToLower().Equals("/xsa"))
            {
                base.Configuration.DoStateTransitionAnalysis = true;
            }
            else
            {
                base.ParseOption(option);
            }
        }

        /// <summary>
        /// Checks for parsing errors.
        /// </summary>
        protected override void CheckForParsingErrors()
        {
            if (base.Configuration.SolutionFilePath.Equals(""))
            {
                IO.Error.ReportAndExit("Please give a valid solution path.");
            }
        }

        /// <summary>
        /// Updates the configuration depending on the
        /// user specified options.
        /// </summary>
        protected override void UpdateConfiguration()
        {
            if (base.Configuration.AnalyzeDataRaces)
            {
                base.Configuration.AnalyzeDataFlow = true;
            }
        }

        /// <summary>
        /// Shows help.
        /// </summary>
        protected override void ShowHelp()
        {
            string help = "\n";

            help += " --------------";
            help += "\n Basic options:";
            help += "\n --------------";
            help += "\n  /?\t\t Show this help menu";
            help += "\n  /s:[x]\t Path to a P# solution";
            help += "\n  /p:[x]\t Name of a project in the P# solution";
            help += "\n  /o:[x]\t Path for output files";
            help += "\n  /timeout:[x]\t Timeout for the tool (default is no timeout)";
            help += "\n  /v:[x]\t Enable verbose mode (values from '1' to '3')";
            help += "\n  /warnings-on\t Show warnings";
            help += "\n  /debug\t Enable debugging";

            help += "\n\n --------------------";
            help += "\n Compilation options:";
            help += "\n --------------------";
            help += "\n  /t:[x]\t The compilation target ('exe', 'lib' or 'test')";

            help += "\n\n --------------------";
            help += "\n Analysis options:";
            help += "\n --------------------";
            help += "\n  /dfa\t\t Enables data-flow analysis";
            help += "\n  /check-races\t Enables race-checking analysis";

            help += "\n";

            IO.PrettyPrintLine(help);
        }

        #endregion
    }
}
