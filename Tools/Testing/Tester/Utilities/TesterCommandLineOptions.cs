﻿//-----------------------------------------------------------------------
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

using System;

using Microsoft.PSharp.IO;

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
            else if (option.ToLower().StartsWith("/method:") && option.Length > 8)
            {
                base.Configuration.TestMethodName = option.Substring(8);
            }
            else if (option.ToLower().Equals("/interactive"))
            {
                base.Configuration.SchedulingStrategy = SchedulingStrategy.Interactive;
            }
            else if (option.ToLower().StartsWith("/sch:"))
            {
                string scheduler = option.ToLower().Substring(5);
                if (scheduler.ToLower().Equals("portfolio"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.Portfolio;
                }
                else if (scheduler.ToLower().Equals("random"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.Random;
                }
                else if (scheduler.ToLower().Equals("roundrobin"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.RoundRobin;
                }
                else if (scheduler.StartsWith("probabilistic"))
                {
                    int i = 0;
                    if (scheduler.Equals("probabilistic") ||
                        !int.TryParse(scheduler.Substring(14), out i) && i >= 0)
                    {
                        Error.ReportAndExit("Please give a valid number of coin " +
                            "flip bound '/sch:probabilistic:[bound]', where [bound] >= 0.");
                    }

                    base.Configuration.SchedulingStrategy = SchedulingStrategy.ProbabilisticRandom;
                    base.Configuration.CoinFlipBound = i;
                }
                else if (scheduler.StartsWith("pct"))
                {
                    int i = 0;
                    if (scheduler.Equals("pct") ||
                        !int.TryParse(scheduler.Substring(4), out i) && i >= 0)
                    {
                        Error.ReportAndExit("Please give a valid number of priority " +
                            "switch bound '/sch:pct:[bound]', where [bound] >= 0.");
                    }

                    base.Configuration.SchedulingStrategy = SchedulingStrategy.PCT;
                    base.Configuration.PrioritySwitchBound = i;
                }
                else if (scheduler.StartsWith("fairpct"))
                {
                    int i = 0;
                    if (scheduler.Equals("fairpct") ||
                        !int.TryParse(scheduler.Substring("fairpct:".Length), out i) && i >= 0)
                    {
                        Error.ReportAndExit("Please give a valid number of priority " +
                            "switch bound '/sch:fairpct:[bound]', where [bound] >= 0.");
                    }

                    base.Configuration.SchedulingStrategy = SchedulingStrategy.FairPCT;
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
                        Error.ReportAndExit("Please give a valid delay " +
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
                        Error.ReportAndExit("Please give a valid delay " +
                            "bound '/sch:rdb:[bound]', where [bound] >= 0.");
                    }

                    base.Configuration.SchedulingStrategy = SchedulingStrategy.RandomDelayBounding;
                    base.Configuration.DelayBound = i;
                }
                else if (scheduler.ToLower().Equals("macemc"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.MaceMC;
                }
                else
                {
                    Error.ReportAndExit("Please give a valid scheduling strategy " +
                        "'/sch:[x]', where [x] is 'random', 'pct' or 'dfs' (other " +
                        "experimental strategies also exist, but are not listed here).");
                }
            }
            else if (option.ToLower().StartsWith("/i:") && option.Length > 3)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(3), out i) && i > 0)
                {
                    Error.ReportAndExit("Please give a valid number of " +
                        "iterations '/i:[x]', where [x] > 0.");
                }

                base.Configuration.SchedulingIterations = i;
            }
            else if (option.ToLower().StartsWith("/parallel:") && option.Length > 10)
            {
                uint i = 0;
                if (!uint.TryParse(option.Substring(10), out i) || i <= 1)
                {
                    Error.ReportAndExit("Please give a valid number of " +
                        "parallel tasks '/parallel:[x]', where [x] > 1.");
                }

                base.Configuration.ParallelBugFindingTasks = i;
            }
            else if (option.ToLower().StartsWith("/run-as-parallel-testing-task"))
            {
                base.Configuration.RunAsParallelBugFindingTask = true;
            }
            else if (option.ToLower().StartsWith("/testing-scheduler-endpoint:") && option.Length > 28)
            {
                string endpoint = option.Substring(28);
                if (endpoint.Length != 36)
                {
                    Error.ReportAndExit("Please give a valid testing scheduler endpoint " +
                        "'/testing-scheduler-endpoint:[x]', where [x] is a unique GUID.");
                }

                base.Configuration.TestingSchedulerEndPoint = endpoint;
            }
            else if (option.ToLower().StartsWith("/testing-scheduler-process-id:") && option.Length > 30)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(30), out i) && i >= 0)
                {
                    Error.ReportAndExit("Please give a valid testing scheduler " +
                        "process id '/testing-scheduler-process-id:[x]', where [x] >= 0.");
                }

                base.Configuration.TestingSchedulerProcessId = i;
            }
            else if (option.ToLower().StartsWith("/testing-process-id:") && option.Length > 20)
            {
                uint i = 0;
                if (!uint.TryParse(option.Substring(20), out i) && i >= 0)
                {
                    Error.ReportAndExit("Please give a valid testing " +
                        "process id '/testing-process-id:[x]', where [x] >= 0.");
                }

                base.Configuration.TestingProcessId = i;
            }
            else if (option.ToLower().Equals("/explore"))
            {
                base.Configuration.PerformFullExploration = true;
            }
            else if (option.ToLower().Equals("/coverage-report"))
            {
                base.Configuration.ReportCodeCoverage = true;
            }
            else if (option.ToLower().Equals("/coverage-debug"))
            {
                base.Configuration.ReportCodeCoverage = true;
                base.Configuration.DebugCodeCoverage = true;
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
                    Error.ReportAndExit("Please give a valid random scheduling " +
                        "seed '/sch-seed:[x]', where [x] is a signed 32-bit integer.");
                }

                base.Configuration.RandomSchedulingSeed = seed;
            }
            else if (option.ToLower().StartsWith("/max-steps:") && option.Length > 11)
            {
                int i = 0;
                int j = 0;
                var tokens = option.Split(new char[] { ':' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 3 || tokens.Length <= 1)
                {
                    Error.ReportAndExit("Invalid number of options supplied via '/max-steps'.");
                }

                if (tokens.Length >= 2)
                {
                    if (!int.TryParse(tokens[1], out i) && i >= 0)
                    {
                        Error.ReportAndExit("Please give a valid number of max scheduling " +
                            " steps to explore '/max-steps:[x]', where [x] >= 0.");
                    }
                }

                if (tokens.Length == 3)
                {
                    if (!int.TryParse(tokens[2], out j) && j >= 0)
                    {
                        Error.ReportAndExit("Please give a valid number of max scheduling " +
                            " steps to explore '/max-steps:[x]:[y]', where [y] >= 0.");
                    }

                    base.Configuration.UserExplicitlySetMaxFairSchedulingSteps = true;
                }
                else
                {
                    j = 10 * i;
                }
                
                base.Configuration.MaxUnfairSchedulingSteps = i;
                base.Configuration.MaxFairSchedulingSteps = j;
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
                    Error.ReportAndExit("Please give a valid safety prefix " +
                        "bound '/prefix:[x]', where [x] >= 0.");
                }

                base.Configuration.SafetyPrefixBound = i;
            }
            else if (option.ToLower().StartsWith("/liveness-temperature-threshold:") && option.Length > 32)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(32), out i) && i >= 0)
                {
                    Error.ReportAndExit("Please give a valid liveness temperature threshold " +
                        "'/liveness-temperature-threshold:[x]', where [x] >= 0.");
                }

                base.Configuration.LivenessTemperatureThreshold = i;
            }
            else if (option.ToLower().Equals("/state-caching"))
            {
                base.Configuration.CacheProgramState = true;
            }
            else if (option.ToLower().Equals("/cycle-replay"))
            {
                base.Configuration.EnableCycleReplayingStrategy = true;
            }
            else if (option.ToLower().Equals("/user-hash"))
            {
                base.Configuration.UserHash = true;
            }
            else if (option.ToLower().Equals("/dynamic-event-reordering"))
            {
                base.Configuration.DynamicEventQueuePrioritization = true;
            }
            else if (option.ToLower().Equals("/visualize"))
            {
                Error.ReportAndExit("Command line option '/visualize' is deprecated. " +
                    "Please use '/coverage-report' instead.");
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
                Error.ReportAndExit("Please give a valid path to a P# " +
                    "program's dll using '/test:[x]'.");
            }

            if (base.Configuration.SchedulingStrategy != SchedulingStrategy.Interactive &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.Portfolio &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.Random &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.ProbabilisticRandom &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.DFS &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.IDDFS &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.DelayBounding &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.RandomDelayBounding &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.PCT &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.FairPCT &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.MaceMC)
            {
                Error.ReportAndExit("Please give a valid scheduling strategy " +
                        "'/sch:[x]', where [x] is 'random' or 'pct' (other experimental " +
                        "strategies also exist, but are not listed here).");
            }

            if (base.Configuration.MaxFairSchedulingSteps < base.Configuration.MaxUnfairSchedulingSteps)
            {
                Error.ReportAndExit("For the option '/max-steps:[N]:[M]', please make sure that [M] >= [N].");
            }

            if (base.Configuration.SafetyPrefixBound > 0 &&
                base.Configuration.SafetyPrefixBound >= base.Configuration.MaxUnfairSchedulingSteps)
            {
                Error.ReportAndExit("Please give a safety prefix bound that is less than the " +
                    "max scheduling steps bound.");
            }

            if (base.Configuration.SchedulingStrategy.Equals("iddfs") &&
                base.Configuration.MaxUnfairSchedulingSteps == 0)
            {
                Error.ReportAndExit("The Iterative Deepening DFS scheduler ('iddfs') " +
                    "must have a max scheduling steps bound, which can be given using " +
                    "'/max-steps:[bound]', where [bound] > 0.");
            }
        }

        /// <summary>
        /// Updates the configuration depending on the
        /// user specified options.
        /// </summary>
        protected override void UpdateConfiguration()
        {
            if (base.Configuration.EnableCycleReplayingStrategy)
            {
                base.Configuration.CacheProgramState = true;
            }

            if (base.Configuration.LivenessTemperatureThreshold == 0)
            {
                if (base.Configuration.CacheProgramState)
                {
                    base.Configuration.LivenessTemperatureThreshold = 100;
                }
                else if (base.Configuration.MaxFairSchedulingSteps > 0)
                {
                    base.Configuration.LivenessTemperatureThreshold =
                        base.Configuration.MaxFairSchedulingSteps / 2;
                }
            }

            if (base.Configuration.RandomSchedulingSeed == null)
            {
                base.Configuration.RandomSchedulingSeed = DateTime.Now.Millisecond;
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
            help += "\n  /test:[x]\t Path to the P# program to test";
            help += "\n  /method:[x]\t Suffix of the test method to execute";
            help += "\n  /timeout:[x]\t Timeout in seconds (disabled by default)";
            help += "\n  /v:[x]\t Enable verbose mode (values from '1' to '3')";
            help += "\n  /o:[x]\t Dump output to directory x (absolute path or relative to current directory)";

            help += "\n\n ---------------------------";
            help += "\n Systematic testing options:";
            help += "\n ---------------------------";
            help += "\n  /i:[x]\t\t Number of schedules to explore for bugs";
            help += "\n  /parallel:[x]\t\t Number of parallel testing tasks ('1' by default)";
            help += "\n  /sch:[x]\t\t Choose a systematic testing strategy ('random' by default)";
            help += "\n  /max-steps:[x]\t Max scheduling steps to be explored (disabled by default)";

            help += "\n\n ---------------------------";
            help += "\n Testing code coverage options:";
            help += "\n ---------------------------";
            help += "\n  /coverage-report\t Print code coverage statistics";

            help += "\n";

            Output.WriteLine(help);
        }

        #endregion
    }
}
