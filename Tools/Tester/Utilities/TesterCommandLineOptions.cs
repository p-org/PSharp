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

using Microsoft.PSharp.SystematicTesting;

namespace Microsoft.PSharp.Utilities
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

        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parse the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected override void ParseOption(string option)
        {
            if (option.ToLower().StartsWith("/test:") && option.Length > 6)
            {
                base.Configuration.AssemblyToBeAnalyzed = option.Substring(6);
            }
            else if (option.ToLower().Equals("/interactive"))
            {
                base.Configuration.SchedulingStrategy = SchedulingStrategy.Interactive;
            }
            else if (option.ToLower().StartsWith("/sch:") && option.Length > 5)
            {
                if (option.ToLower().Substring(5).Equals("random"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.Random;
                }
                else if (option.ToLower().Substring(5).Equals("dfs"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.DFS;
                }
                else if (option.ToLower().Substring(5).Equals("iddfs"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.IDDFS;
                }
                else if (option.ToLower().Substring(5).Equals("db"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.DelayBounding;
                }
                else if (option.ToLower().Substring(5).Equals("rdb"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.RandomDelayBounding;
                }
                else if (option.ToLower().Substring(5).Equals("pct"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.PCT;
                }
                else if (option.ToLower().Substring(5).Equals("rob"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.RandomOperationBounding;
                }
                else if (option.ToLower().Substring(5).Equals("pob"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.PrioritizedOperationBounding;
                }
                else if (option.ToLower().Substring(5).Equals("macemc"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.MaceMC;
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

                base.Configuration.SchedulingIterations = i;
            }
            else if (option.ToLower().Equals("/explore"))
            {
                base.Configuration.FullExploration = true;
            }
            else if (option.ToLower().StartsWith("/sch-seed:") && option.Length > 10)
            {
                int seed;
                if (!int.TryParse(option.Substring(10), out seed))
                {
                    ErrorReporter.ReportAndExit("Please give a valid random scheduling " +
                        "seed '/sch-seed:[x]', where [x] is a signed 32-bit integer.");
                }

                base.Configuration.RandomSchedulingSeed = seed;
            }
            else if (option.ToLower().StartsWith("/depth-bound:") && option.Length > 13)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(13), out i) && i >= 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid exploration depth " +
                        "bound '/depth-bound:[x]', where [x] >= 0.");
                }

                base.Configuration.DepthBound = i;
            }
            else if (option.ToLower().Equals("/depthboundbug"))
            {
                base.Configuration.ConsiderDepthBoundHitAsBug = true;
            }
            else if (option.ToLower().StartsWith("/bug-depth:") && option.Length > 11)
            {
                int i = 1;
                if (!int.TryParse(option.Substring(11), out i) && i >= 1)
                {
                    ErrorReporter.ReportAndExit("Please give a valid bug depth " +
                        "'/bug-depth:[x]', where [x] >= 1.");
                }

                base.Configuration.BugDepth = i;
            }
            else if (option.ToLower().StartsWith("/delay-bound:") && option.Length > 13)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(13), out i) && i >= 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid delay bound " +
                        "'/delay-bound:[x]', where [x] >= 0.");
                }

                base.Configuration.DelayBound = i;
            }
            else if (option.ToLower().StartsWith("/prefix:") && option.Length > 8)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(8), out i) && i >= 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid safety prefix " +
                        "bound '/prefix:[x]', where [x] >= 0.");
                }

                base.Configuration.SafetyPrefixBound = i;
            }
            else if (option.ToLower().Equals("/print-trace"))
            {
                base.Configuration.PrintTrace = true;
            }
            else if (option.ToLower().Equals("/tpl"))
            {
                base.Configuration.ScheduleIntraMachineConcurrency = true;
            }
            else if (option.ToLower().Equals("/liveness"))
            {
                base.Configuration.CheckLiveness = true;
            }
            else if (option.ToLower().Equals("/state-caching"))
            {
                base.Configuration.CacheProgramState = true;
            }
            else if (option.ToLower().Equals("/opbound"))
            {
                base.Configuration.BoundOperations = true;
            }
            else if (option.ToLower().Equals("/dynamic-event-reordering"))
            {
                base.Configuration.DynamicEventQueuePrioritization = true;
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
            if (base.Configuration.AssemblyToBeAnalyzed.Equals(""))
            {
                ErrorReporter.ReportAndExit("Please give a valid path to a P# program's dll.");
            }

            if (base.Configuration.SchedulingStrategy != SchedulingStrategy.Interactive &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.Random &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.DFS &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.IDDFS &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.DelayBounding &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.RandomDelayBounding &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.PCT &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.RandomOperationBounding &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.PrioritizedOperationBounding &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.MaceMC)
            {
                ErrorReporter.ReportAndExit("Please give a valid scheduling strategy " +
                    "'/sch:[x]', where [x] is 'random', 'dfs', 'iddfs', 'db' or 'rdb'.");
            }
            
            if (base.Configuration.SafetyPrefixBound > 0 &&
                base.Configuration.SafetyPrefixBound >= base.Configuration.DepthBound)
            {
                ErrorReporter.ReportAndExit("Please give a safety prefix bound that is less than the " +
                    "max depth bound.");
            }

            if (base.Configuration.SchedulingStrategy.Equals("iddfs") && base.Configuration.DepthBound == 0)
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
            help += "\n  /v:[x]\t Enable verbose mode (values from '1' to '3')";
            help += "\n  /debug\t Enable debugging";

            help += "\n\n---------------------------";
            help += "\nSystematic testing options:";
            help += "\n---------------------------";
            help += "\n  /i:[x]\t\t Number of schedules to explore for bugs";
            help += "\n  /interactive\t\t Enable interactive scheduling";
            help += "\n  /sch:[x]\t\t Choose a systematic testing strategy ('random' by default)";
            help += "\n  /depth-bound:[x]\t Depth bound to be explored ('10000' by default)";
            help += "\n  /delay-bound:[x]\t Delay bound ('2' by default)";
            help += "\n  /liveness\t\t Enable liveness property checking";
            help += "\n  /sch-seed:[x]\t\t Choose a scheduling seed (signed 32-bit integer)";

            help += "\n\n---------------------------";
            help += "\nExperimental options:";
            help += "\n---------------------------";
            help += "\n  /tpl\t Enable intra-machine concurrency scheduling";

            help += "\n";

            IO.PrettyPrintLine(help);
        }

        #endregion
    }
}
