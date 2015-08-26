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

namespace Microsoft.PSharp.Tooling
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
            base.Configuration = new LanguageServicesConfiguration();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parse the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected override void ParseOption(string option)
        {
            var configuration = base.Configuration as LanguageServicesConfiguration;
            if (option.ToLower().Equals("/testing"))
            {
                configuration.CompileForTesting = true;
            }
            else if (option.ToLower().Equals("/distributed"))
            {
                configuration.CompileForDistribution = true;
            }
            else if (option.ToLower().Equals("/analyze"))
            {
                configuration.RunStaticAnalysis = true;
            }
            else if (option.ToLower().Equals("/showgivesup"))
            {
                configuration.ShowGivesUpInformation = true;
            }
            else if (option.ToLower().Equals("/time"))
            {
                configuration.ShowRuntimeResults = true;
            }
            else if (option.ToLower().Equals("/timedfa"))
            {
                configuration.ShowDFARuntimeResults = true;
            }
            else if (option.ToLower().Equals("/timeroa"))
            {
                configuration.ShowROARuntimeResults = true;
            }
            else if (option.ToLower().Equals("/nostatetransitionanalysis"))
            {
                configuration.DoStateTransitionAnalysis = false;
            }
            else if (option.ToLower().Equals("/analyzeexceptions"))
            {
                configuration.AnalyzeExceptionHandling = true;
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
            var configuration = base.Configuration as LanguageServicesConfiguration;
            if (configuration.SolutionFilePath.Equals(""))
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
            help += "\n  /testing\t Compile the P# program for testing";
            help += "\n  /ditributed\t Compile the P# program using the distributed runtime";

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
