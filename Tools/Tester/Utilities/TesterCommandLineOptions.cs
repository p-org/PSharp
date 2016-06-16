//-----------------------------------------------------------------------
// <copyright file="TesterCommandLineOptions.cs">
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
        /// Parses the given option.
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
            else if (option.ToLower().StartsWith("/sch:"))
            {
                string scheduler = option.ToLower().Substring(5);
                if (scheduler.ToLower().Equals("random"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.Random;
                }
                else if (scheduler.StartsWith("random-coin"))
                {
                    int i = 0;
                    if (scheduler.Equals("random-coin") ||
                        !int.TryParse(scheduler.Substring(12), out i) && i >= 0)
                    {
                        ErrorReporter.ReportAndExit("Please give a valid number of coin " +
                            "flip bound '/sch:random-coin:[bound]', where [bound] >= 0.");
                    }

                    base.Configuration.SchedulingStrategy = SchedulingStrategy.RandomCoin;
                    base.Configuration.CoinFlipBound = i;
                }
                else if (scheduler.StartsWith("pct"))
                {
                    int i = 0;
                    if (scheduler.Equals("pct") ||
                        !int.TryParse(scheduler.Substring(4), out i) && i >= 0)
                    {
                        ErrorReporter.ReportAndExit("Please give a valid number of priority " +
                            "switch bound '/sch:pct:[bound]', where [bound] >= 0.");
                    }

                    base.Configuration.SchedulingStrategy = SchedulingStrategy.PCT;
                    base.Configuration.PrioritySwitchBound = i;
                }
                else if (scheduler.ToLower().Equals("dfs"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.DFS;
                }
                else if (scheduler.ToLower().Equals("iddfs"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.IDDFS;
                }
                else if (scheduler.StartsWith("db"))
                {
                    int i = 0;
                    if (scheduler.Equals("db") ||
                        !int.TryParse(scheduler.Substring(3), out i) && i >= 0)
                    {
                        ErrorReporter.ReportAndExit("Please give a valid delay " +
                            "bound '/sch:db:[bound]', where [bound] >= 0.");
                    }

                    base.Configuration.SchedulingStrategy = SchedulingStrategy.DelayBounding;
                    base.Configuration.DelayBound = i;
                }
                else if (scheduler.StartsWith("rdb"))
                {
                    int i = 0;
                    if (scheduler.Equals("rdb") ||
                        !int.TryParse(scheduler.Substring(4), out i) && i >= 0)
                    {
                        ErrorReporter.ReportAndExit("Please give a valid delay " +
                            "bound '/sch:rdb:[bound]', where [bound] >= 0.");
                    }

                    base.Configuration.SchedulingStrategy = SchedulingStrategy.RandomDelayBounding;
                    base.Configuration.DelayBound = i;
                }
                else if (scheduler.ToLower().Equals("rob"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.RandomOperationBounding;
                    base.Configuration.BoundOperations = true;
                }
                else if (scheduler.StartsWith("pob"))
                {
                    int i = 0;
                    if (scheduler.Equals("pob") ||
                        !int.TryParse(scheduler.Substring(4), out i) && i >= 0)
                    {
                        ErrorReporter.ReportAndExit("Please give a valid number of priority " +
                            "switch points '/sch:pob:[x]', where [x] >= 0.");
                    }

                    base.Configuration.SchedulingStrategy = SchedulingStrategy.PrioritizedOperationBounding;
                    base.Configuration.BoundOperations = true;
                    base.Configuration.PrioritySwitchBound = i;
                }
                else if (scheduler.ToLower().Equals("macemc"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.MaceMC;
                }
                else
                {
                    ErrorReporter.ReportAndExit("Please give a valid scheduling strategy " +
                        "'/sch:[x]', where [x] is 'random', 'pct' or 'dfs' (other " +
                        "experimental strategies also exist, but are not listed here).");
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
                base.Configuration.PerformFullExploration = true;
            }
            else if (option.ToLower().Equals("/visualize"))
            {
                base.Configuration.EnableVisualization = true;
            }
            else if (option.ToLower().Equals("/detect-races"))
            {
                base.Configuration.EnableDataRaceDetection = true;
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
            else if (option.ToLower().StartsWith("/max-steps:") && option.Length > 11)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(11), out i) && i >= 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid number of max scheduling " +
                        " steps to explore '/max-steps:[x]', where [x] >= 0.");
                }

                base.Configuration.DepthBound = i;
            }
            else if (option.ToLower().Equals("/depth-bound-bug"))
            {
                base.Configuration.ConsiderDepthBoundHitAsBug = true;
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
                ErrorReporter.ReportAndExit("Please give a valid path to a P# " +
                    "program's dll using '/test:[x]'.");
            }

            if (base.Configuration.SchedulingStrategy != SchedulingStrategy.Interactive &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.Random &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.RandomCoin &&
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
                        "'/sch:[x]', where [x] is 'random', 'pct' or 'dfs' (other " +
                        "experimental strategies also exist, but are not listed here).");
            }
            
            if (base.Configuration.SafetyPrefixBound > 0 &&
                base.Configuration.SafetyPrefixBound >= base.Configuration.DepthBound)
            {
                ErrorReporter.ReportAndExit("Please give a safety prefix bound that is less than the " +
                    "max scheduling steps bound.");
            }

            if (base.Configuration.SchedulingStrategy.Equals("iddfs") &&
                base.Configuration.DepthBound == 0)
            {
                ErrorReporter.ReportAndExit("The Iterative Deepening DFS scheduler ('iddfs') " +
                    "must have a max scheduling steps bound, which can be given using " +
                    "'/max-steps:[bound]', where [bound] > 0.");
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
            help += "\n  /test:[x]\t Name of a project in the P# solution to test";
            help += "\n  /o:[x]\t Path for output files";
            help += "\n  /timeout:[x]\t Timeout (default is no timeout)";
            help += "\n  /v:[x]\t Enable verbose mode (values from '1' to '3')";
            help += "\n  /debug\t Enable debugging";

            help += "\n\n ---------------------------";
            help += "\n Systematic testing options:";
            help += "\n ---------------------------";
            help += "\n  /i:[x]\t Number of schedules to explore for bugs";
            help += "\n  /interactive\t Enable interactive scheduling";
            help += "\n  /sch:[x]\t Choose a systematic testing strategy ('random' by default)";
            help += "\n  /max-steps:[x] Max scheduling steps to be explored ('10000' by default)";
            help += "\n  /sch-seed:[x]\t Choose a scheduling seed (signed 32-bit integer)";

            help += "\n\n ----------------------------";
            help += "\n Data race detection options:";
            help += "\n ----------------------------";
            help += "\n  /detect-races\t Enable data-race detection";

            help += "\n\n ---------------------";
            help += "\n Experimental options:";
            help += "\n ---------------------";
            help += "\n  /tpl\t Enable intra-machine concurrency scheduling";

            help += "\n";

            IO.PrettyPrintLine(help);
        }

        #endregion
    }
}
