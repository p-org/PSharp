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
                    base.Configuration.CompilationTargets.Clear();
                    base.Configuration.CompilationTargets.Add(CompilationTarget.Execution);
                }
                else if (option.ToLower().Substring(3).Equals("lib"))
                {
                    base.Configuration.CompilationTargets.Clear();
                    base.Configuration.CompilationTargets.Add(CompilationTarget.Library);
                }
                else if (option.ToLower().Substring(3).Equals("test"))
                {
                    base.Configuration.CompilationTargets.Clear();
                    base.Configuration.CompilationTargets.Add(CompilationTarget.Testing);
                }
                else if (option.ToLower().Substring(3).Equals("remote"))
                {
                    base.Configuration.CompilationTargets.Clear();
                    base.Configuration.CompilationTargets.Add(CompilationTarget.Remote);
                }
                else
                {
                    ErrorReporter.ReportAndExit("Please give a valid compilation target '/t:[x]', " +
                        "where [x] is 'all', 'exe', 'lib' or 'test'.");
                }
            }
            else if (option.ToLower().Equals("/analyze"))
            {
                base.Configuration.RunStaticAnalysis = true;
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
                    ErrorReporter.ReportAndExit("Please give a valid data-flow information " +
                        "level '/emit-data-flow:[x]', where [x] is 'default' or 'full'.");
                }
            }
            else if (option.ToLower().Equals("/time"))
            {
                base.Configuration.TimeStaticAnalysis = true;
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
                ErrorReporter.ReportAndExit("Please give a valid solution path.");
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

            help += "\n\n ---------------------";
            help += "\n Experimental options:";
            help += "\n ---------------------";
            help += "\n  /tpl\t\t Enable intra-machine concurrency scheduling";

            help += "\n";

            IO.PrettyPrintLine(help);
        }

        #endregion
    }
}
