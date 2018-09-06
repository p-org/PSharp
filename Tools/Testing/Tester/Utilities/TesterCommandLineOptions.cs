// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Utilities
{
    public sealed class TesterCommandLineOptions : BaseCommandLineOptions
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public TesterCommandLineOptions(string[] args)
            : base (args)
        {

        }

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
            else if (option.ToLower().StartsWith("/runtime:") && option.Length > 9)
            {
                base.Configuration.TestingRuntimeAssembly = option.Substring(9);
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
                else if (scheduler.ToLower().Equals("dpor"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.DPOR;
                }
                else if (scheduler.ToLower().Equals("rdpor"))
                {
                    base.Configuration.SchedulingStrategy = SchedulingStrategy.RDPOR;
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
                else
                {
                    Error.ReportAndExit("Please give a valid scheduling strategy " +
                        "'/sch:[x]', where [x] is 'random', 'pct' or 'dfs' (other " +
                        "experimental strategies also exist, but are not listed here).");
                }
            }
            else if (option.ToLower().StartsWith("/replay:") && option.Length > 8)
            {
                string extension = System.IO.Path.GetExtension(option.Substring(8));
                if (!extension.Equals(".schedule"))
                {
                    Error.ReportAndExit("Please give a valid schedule file " +
                        "'/replay:[x]', where [x] has extension '.schedule'.");
                }

                base.Configuration.ScheduleFile = option.Substring(8);
            }
            else if (option.ToLower().StartsWith("/reduction:"))
            {
                string reduction = option.ToLower().Substring(11);
                if (reduction.ToLower().Equals("none"))
                {
                    base.Configuration.ReductionStrategy = ReductionStrategy.None;
                }
                else if (reduction.ToLower().Equals("omit"))
                {
                    base.Configuration.ReductionStrategy = ReductionStrategy.OmitSchedulingPoints;
                }
                else if (reduction.ToLower().Equals("force"))
                {
                    base.Configuration.ReductionStrategy = ReductionStrategy.ForceSchedule;
                }
                else
                {
                    Error.ReportAndExit("Please give a valid reduction strategy " +
                        "'/reduction:[x]', where [x] is 'none', 'omit' or 'force'.");
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
            else if (option.ToLower().Equals("/coverage"))
            {
                base.Configuration.ReportCodeCoverage = true;
                base.Configuration.ReportActivityCoverage = true;
            }
            else if (option.ToLower().Equals("/coverage:code"))
            {
                base.Configuration.ReportCodeCoverage = true;
            }
            else if (option.ToLower().Equals("/coverage:activity"))
            {
                base.Configuration.ReportActivityCoverage = true;
            }
            else if (option.ToLower().Equals("/coverage:activity-debug"))
            {
                base.Configuration.ReportActivityCoverage = true;
                base.Configuration.DebugActivityCoverage = true;
            }
            else if (option.ToLower().StartsWith("/instr:"))
            {
                base.Configuration.AdditionalCodeCoverageAssemblies[option.Substring(7)] = false;
            }
            else if (option.ToLower().StartsWith("/instr-list:"))
            {
                base.Configuration.AdditionalCodeCoverageAssemblies[option.Substring(12)] = true;
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
            else if (option.ToLower().Equals("/cycle-detection"))
            {
                base.Configuration.EnableCycleDetection = true;
            }
            else if (option.ToLower().Equals("/custom-state-hashing"))
            {
                base.Configuration.EnableUserDefinedStateHashing = true;
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
                base.Configuration.SchedulingStrategy != SchedulingStrategy.PCT &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.FairPCT &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.DFS &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.IDDFS &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.DPOR &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.RDPOR &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.DelayBounding &&
                base.Configuration.SchedulingStrategy != SchedulingStrategy.RandomDelayBounding)
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

#if NETCOREAPP2_1
            if (base.Configuration.ParallelBugFindingTasks > 1)
            {
                Error.ReportAndExit("We do not yet support parallel testing when using the .NET Core runtime.");
            }

            if (base.Configuration.ReportCodeCoverage || base.Configuration.ReportActivityCoverage)
            {
                Error.ReportAndExit("We do not yet support coverage reports when using the .NET Core runtime.");
            }
#endif
        }

        /// <summary>
        /// Updates the configuration depending on the
        /// user specified options.
        /// </summary>
        protected override void UpdateConfiguration()
        {
            if (base.Configuration.LivenessTemperatureThreshold == 0)
            {
                if (base.Configuration.EnableCycleDetection)
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
            help += "\n  /replay:[x]\t Tries to replay the schedule, and then switches to the specified strategy";

            help += "\n\n ---------------------------";
            help += "\n Testing code coverage options:";
            help += "\n ---------------------------";
            help += "\n  /coverage:code\t Generate code coverage statistics (via VS instrumentation)";
            help += "\n  /coverage:activity\t Generate activity (machine, event, etc.) coverage statistics";
            help += "\n  /coverage\t Generate both code and activity coverage statistics";
            help += "\n  /coverage:activity-debug\t Print activity coverage statistics with debug info";
            help += "\n  /instr:[filespec]\t Additional file spec(s) to instrument for /coverage:code; wildcards supported";
            help += "\n  /instr-list:[listfilename]\t File containing the names of additional file(s), one per line,";
            help += "\n         wildcards supported, to instrument for /coverage:code; lines starting with '//' are skipped";

            help += "\n";

            Output.WriteLine(help);
        }
    }
}
