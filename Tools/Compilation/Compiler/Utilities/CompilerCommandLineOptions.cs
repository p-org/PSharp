// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Utilities
{
    public sealed class CompilerCommandLineOptions : BaseCommandLineOptions
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public CompilerCommandLineOptions(string[] args)
            : base (args)
        {
        }

        /// <summary>
        /// Parses the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected override void ParseOption(string option)
        {
            if (this.IsMatch(option, @"[\/|-]t:") && option.Length > 3)
            {
                if (this.IsMatch(option.Substring(14), @"exe$"))
                {
                    base.Configuration.CompilationTarget = CompilationTarget.Execution;
                }
                else if (this.IsMatch(option.Substring(14), @"lib$"))
                {
                    base.Configuration.CompilationTarget = CompilationTarget.Library;
                }
                else if (this.IsMatch(option.Substring(14), @"test$"))
                {
                    base.Configuration.CompilationTarget = CompilationTarget.Testing;
                }
                else if (this.IsMatch(option.Substring(14), @"remote$"))
                {
                    base.Configuration.CompilationTarget = CompilationTarget.Remote;
                }
                else
                {
                    Error.ReportAndExit("Please give a valid compilation target " +
                        "'/t:[x]', where [x] is 'all', 'exe', 'lib' or 'test'.");
                }
            }
            else if (this.IsMatch(option, @"[\/|-]optimization:") && option.Length > 14)
            {
                if (this.IsMatch(option.Substring(14), @"debug$"))
                {
                    base.Configuration.OptimizationTarget = OptimizationTarget.Debug;
                }
                else if (this.IsMatch(option.Substring(14), @"release$"))
                {
                    base.Configuration.OptimizationTarget = OptimizationTarget.Release;
                }
                else
                {
                    Error.ReportAndExit("Please give a valid optimization target " +
                        "'/optimization:[x]', where [x] is 'debug' or 'release'.");
                }
            }
            else if (this.IsMatch(option, @"[\/|-]pass:") && option.Length > 6)
            {
                if (!option.ToLower().Substring(6).EndsWith(".dll"))
                {
                    Error.ReportAndExit("Please give a valid custom compiler pass dll " +
                        "'/pass:[x]', where [x] is a 'dll'.");
                }

                base.Configuration.CustomCompilerPassAssemblyPaths.Add(option.Substring(6));
            }
            else if (this.IsMatch(option, @"[\/|-]dfa$"))
            {
                base.Configuration.AnalyzeDataFlow = true;
            }
            else if (this.IsMatch(option, @"[\/|-]check-races$"))
            {
                base.Configuration.AnalyzeDataRaces = true;
            }
            else if (this.IsMatch(option, @"[\/|-]emit-control-flow$"))
            {
                base.Configuration.ShowControlFlowInformation = true;
            }
            else if (this.IsMatch(option, @"[\/|-]emit-data-flow$"))
            {
                base.Configuration.ShowDataFlowInformation = true;
            }
            else if (this.IsMatch(option, @"[\/|-]emit-control-flow:") && option.Length > 16)
            {
                if (this.IsMatch(option.Substring(16), @"default$"))
                {
                    base.Configuration.ShowDataFlowInformation = true;
                }
                else if (this.IsMatch(option.Substring(16), @"full$"))
                {
                    base.Configuration.ShowFullDataFlowInformation = true;
                }
                else
                {
                    Error.ReportAndExit("Please give a valid data-flow information " +
                        "level '/emit-data-flow:[x]', where [x] is 'default' or 'full'.");
                }
            }
            else if (this.IsMatch(option, @"[\/|-]time$"))
            {
                base.Configuration.EnableProfiling = true;
            }
            else if (this.IsMatch(option, @"[\/|-]xsa$"))
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
                Error.ReportAndExit("Please give a valid solution path.");
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
            help += "\n  -?\t\t Show this help menu";
            help += "\n  -s:[x]\t Path to a P# solution";
            help += "\n  -p:[x]\t Name of a project in the P# solution";
            help += "\n  -o:[x]\t Path for output files";
            help += "\n  -timeout:[x]\t Timeout for the tool (default is no timeout)";
            help += "\n  -v:[x]\t Enable verbose mode (values from '1' to '3')";
            help += "\n  -warnings-on\t Show warnings";
            help += "\n  -debug\t Enable debugging";

            help += "\n\n --------------------";
            help += "\n Compilation options:";
            help += "\n --------------------";
            help += "\n  -t:[x]\t The compilation target ('exe', 'lib' or 'test')";

            help += "\n\n --------------------";
            help += "\n Analysis options:";
            help += "\n --------------------";
            help += "\n  -dfa\t\t Enables data-flow analysis";
            help += "\n  -check-races\t Enables race-checking analysis";

            help += "\n";

            Output.WriteLine(help);
        }
    }
}
