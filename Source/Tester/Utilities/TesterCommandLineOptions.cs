//-----------------------------------------------------------------------
// <copyright file="TesterCommandLineOptions.cs">
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

using Microsoft.PSharp.DynamicAnalysis;

namespace Microsoft.PSharp.Tooling
{
    public sealed class TesterCommandLineOptions : BaseCommandLineOptions
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public TesterCommandLineOptions(string[] args)
            : base (args)
        {
            base.Configuration = new DynamicAnalysisConfiguration();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parse the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected override void ParseOption(string option)
        {
            var configuration = base.Configuration as DynamicAnalysisConfiguration;
            if (option.ToLower().StartsWith("/test:") && option.Length > 6)
            {
                configuration.AssemblyToBeAnalyzed = option.Substring(6);
            }
            else if (option.ToLower().StartsWith("/sch:") && option.Length > 5)
            {
                if (option.ToLower().Substring(5).Equals("random"))
                {
                    configuration.SchedulingStrategy = SchedulingStrategy.Random;
                }
                else if (option.ToLower().Substring(5).Equals("dfs"))
                {
                    configuration.SchedulingStrategy = SchedulingStrategy.DFS;
                }
                else if (option.ToLower().Substring(5).Equals("iddfs"))
                {
                    configuration.SchedulingStrategy = SchedulingStrategy.IDDFS;
                }
                else if (option.ToLower().Substring(5).Equals("macemc"))
                {
                    configuration.SchedulingStrategy = SchedulingStrategy.MaceMC;
                }
            }
            else if (option.ToLower().StartsWith("/i:") && option.Length > 3)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(3), out i) && i > 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid number of iterations " +
                        "'/i:[x]', where [x] > 0.");
                }

                configuration.SchedulingIterations = i;
            }
            else if (option.ToLower().Equals("/explore"))
            {
                configuration.FullExploration = true;
            }
            else if (option.ToLower().StartsWith("/sch-seed:") && option.Length > 10)
            {
                int seed;
                if (!int.TryParse(option.Substring(10), out seed))
                {
                    ErrorReporter.ReportAndExit("Please give a valid random scheduling " +
                        "seed '/sch-seed:[x]', where [x] is a signed 32-bit integer.");
                }

                configuration.RandomSchedulingSeed = seed;
            }
            else if (option.ToLower().StartsWith("/db:") && option.Length > 4)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(4), out i) && i >= 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid exploration depth " +
                        "bound '/db:[x]', where [x] >= 0.");
                }

                configuration.DepthBound = i;
            }
            else if (option.ToLower().StartsWith("/prefix:") && option.Length > 8)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(8), out i) && i >= 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid safety prefix " +
                        "bound '/prefix:[x]', where [x] >= 0.");
                }

                configuration.SafetyPrefixBound = i;
            }
            else if (option.ToLower().Equals("/printtrace"))
            {
                configuration.PrintTrace = true;
            }
            else if (option.ToLower().Equals("/tpl"))
            {
                configuration.ScheduleIntraMachineConcurrency = true;
            }
            else if (option.ToLower().Equals("/liveness"))
            {
                configuration.CheckLiveness = true;
            }
            else if (option.ToLower().Equals("/statecaching"))
            {
                configuration.CacheProgramState = true;
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
            var configuration = base.Configuration as DynamicAnalysisConfiguration;
            if (configuration.AssemblyToBeAnalyzed.Equals(""))
            {
                ErrorReporter.ReportAndExit("Please give a valid path to a P# program's dll.");
            }

            if (configuration.SchedulingStrategy != SchedulingStrategy.Random &&
                configuration.SchedulingStrategy != SchedulingStrategy.DFS &&
                configuration.SchedulingStrategy != SchedulingStrategy.IDDFS &&
                configuration.SchedulingStrategy != SchedulingStrategy.MaceMC)
            {
                ErrorReporter.ReportAndExit("Please give a valid scheduling strategy " +
                    "'/sch:[x]', where [x] is 'random', 'dfs' or 'iddfs'.");
            }
            
            if (configuration.SafetyPrefixBound > 0 &&
                configuration.SafetyPrefixBound >= configuration.DepthBound)
            {
                ErrorReporter.ReportAndExit("Please give a safety prefix bound that is less than the " +
                    "max depth bound.");
            }

            if (configuration.SchedulingStrategy.Equals("iddfs") && configuration.DepthBound == 0)
            {
                ErrorReporter.ReportAndExit("The Iterative Deepening DFS scheduler ('iddfs') must have a " +
                    "max depth bound. Please give a depth bound using '/db:[x]', where [x] > 0.");
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
            help += "\n  /test:[x]\t Name of a project in the P# solution to test";
            help += "\n  /o:[x]\t Path for output files";
            help += "\n  /timeout:[x]\t Timeout (default is no timeout)";
            help += "\n  /v:[x]\t Enable verbose mode (values from '0' to '3')";
            help += "\n  /debug\t Enable debugging";

            help += "\n\n---------------------------";
            help += "\nSystematic testing options:";
            help += "\n---------------------------";
            help += "\n  /i:[x]\t Number of schedules to explore for bugs";
            help += "\n  /sch:[x]\t Choose a systematic testing strategy ('random' by default)";
            help += "\n  /db:[x]\t Depth bound to be explored ('10000' by default)";
            help += "\n  /liveness\t Enable liveness property checking";
            help += "\n  /sch-seed:[x]\t Choose a scheduling seed (signed 32-bit integer)";

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
