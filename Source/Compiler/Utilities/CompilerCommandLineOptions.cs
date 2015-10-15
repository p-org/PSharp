//-----------------------------------------------------------------------
// <copyright file="CompilerCommandLineOptions.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
            base.Configuration = Configuration.Create();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parse the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected override void ParseOption(string option)
        {
            if (option.ToLower().StartsWith("/t:") && option.Length > 3)
            {
                if (option.ToLower().Substring(3).Equals("execution"))
                {
                    base.Configuration.CompilationTargets.Clear();
                    base.Configuration.CompilationTargets.Add(CompilationTarget.Execution);
                }
                else if (option.ToLower().Substring(3).Equals("testing"))
                {
                    base.Configuration.CompilationTargets.Clear();
                    base.Configuration.CompilationTargets.Add(CompilationTarget.Testing);
                }
                else if (option.ToLower().Substring(3).Equals("distribution"))
                {
                    base.Configuration.CompilationTargets.Clear();
                    base.Configuration.CompilationTargets.Add(CompilationTarget.Distribution);
                }
                else
                {
                    ErrorReporter.ReportAndExit("Please give a valid compilation target '/t:[x]', " +
                        "where [x] is 'all', 'execution', 'testing' or 'distribution'.");
                }
            }
            else if (option.ToLower().Equals("/analyze"))
            {
                base.Configuration.RunStaticAnalysis = true;
            }
            else if (option.ToLower().Equals("/showgivesup"))
            {
                base.Configuration.ShowGivesUpInformation = true;
            }
            else if (option.ToLower().Equals("/time"))
            {
                base.Configuration.ShowRuntimeResults = true;
            }
            else if (option.ToLower().Equals("/timedfa"))
            {
                base.Configuration.ShowDFARuntimeResults = true;
            }
            else if (option.ToLower().Equals("/timeroa"))
            {
                base.Configuration.ShowROARuntimeResults = true;
            }
            else if (option.ToLower().Equals("/nostatetransitionanalysis"))
            {
                base.Configuration.DoStateTransitionAnalysis = false;
            }
            else if (option.ToLower().Equals("/analyzeexceptions"))
            {
                base.Configuration.AnalyzeExceptionHandling = true;
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

            help += "--------------";
            help += "\nBasic options:";
            help += "\n--------------";
            help += "\n  /?\t\t Show this help menu";
            help += "\n  /s:[x]\t Path to a P# solution";
            help += "\n  /p:[x]\t Name of a project in the P# solution";
            help += "\n  /o:[x]\t Path for output files";
            help += "\n  /timeout:[x]\t Timeout for the tool (default is no timeout)";
            help += "\n  /v:[x]\t Enable verbose mode (values from '0' to '3')";
            help += "\n  /debug\t Enable debugging";

            help += "\n\n--------------------";
            help += "\nCompilation options:";
            help += "\n--------------------";
            help += "\n  /t:[x]\t The compilation target (default is 'execution' and 'testing')";

            help += "\n\n---------------------------";
            help += "\nExperimental options:";
            help += "\n---------------------------";
            help += "\n  /tpl\t Enable intra-machine concurrency scheduling";

            help += "\n";

            Output.PrettyPrintLine(help);
        }

        #endregion
    }
}
