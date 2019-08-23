// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Utilities
{
    public sealed class CompilerCommandLineOptions : BaseCommandLineOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerCommandLineOptions"/> class.
        /// </summary>
        public CompilerCommandLineOptions(string[] args)
            : base(args)
        {
        }

        /// <summary>
        /// Parses the given option.
        /// </summary>
        protected override void ParseOption(string option)
        {
            if (IsMatch(option, @"^[\/|-]t:") && option.Length > 3)
            {
                if (IsMatch(option.Substring(14), @"^exe$"))
                {
                    this.Configuration.CompilationTarget = CompilationTarget.Execution;
                }
                else if (IsMatch(option.Substring(14), @"^lib$"))
                {
                    this.Configuration.CompilationTarget = CompilationTarget.Library;
                }
                else if (IsMatch(option.Substring(14), @"^test$"))
                {
                    this.Configuration.CompilationTarget = CompilationTarget.Testing;
                }
                else if (IsMatch(option.Substring(14), @"^remote$"))
                {
                    this.Configuration.CompilationTarget = CompilationTarget.Remote;
                }
                else
                {
                    Error.ReportAndExit("Please give a valid compilation target " +
                        "'/t:[x]', where [x] is 'all', 'exe', 'lib' or 'test'.");
                }
            }
            else if (IsMatch(option, @"^[\/|-]optimization:") && option.Length > 14)
            {
                if (IsMatch(option.Substring(14), @"^debug$"))
                {
                    this.Configuration.OptimizationTarget = OptimizationTarget.Debug;
                }
                else if (IsMatch(option.Substring(14), @"^release$"))
                {
                    this.Configuration.OptimizationTarget = OptimizationTarget.Release;
                }
                else
                {
                    Error.ReportAndExit("Please give a valid optimization target " +
                        "'/optimization:[x]', where [x] is 'debug' or 'release'.");
                }
            }
            else if (IsMatch(option, @"^[\/|-]pass:") && option.Length > 6)
            {
                if (!option.ToLower().Substring(6).EndsWith(".dll"))
                {
                    Error.ReportAndExit("Please give a valid custom compiler pass dll " +
                        "'/pass:[x]', where [x] is a 'dll'.");
                }

                this.Configuration.CustomCompilerPassAssemblyPaths.Add(option.Substring(6));
            }
            else if (IsMatch(option, @"^[\/|-]dfa$"))
            {
                this.Configuration.AnalyzeDataFlow = true;
            }
            else if (IsMatch(option, @"^[\/|-]check-races$"))
            {
                this.Configuration.AnalyzeDataRaces = true;
            }
            else if (IsMatch(option, @"^[\/|-]emit-control-flow$"))
            {
                this.Configuration.ShowControlFlowInformation = true;
            }
            else if (IsMatch(option, @"^[\/|-]emit-data-flow$"))
            {
                this.Configuration.ShowDataFlowInformation = true;
            }
            else if (IsMatch(option, @"^[\/|-]emit-control-flow:") && option.Length > 16)
            {
                if (IsMatch(option.Substring(16), @"^default$"))
                {
                    this.Configuration.ShowDataFlowInformation = true;
                }
                else if (IsMatch(option.Substring(16), @"^full$"))
                {
                    this.Configuration.ShowFullDataFlowInformation = true;
                }
                else
                {
                    Error.ReportAndExit("Please give a valid data-flow information " +
                        "level '/emit-data-flow:[x]', where [x] is 'default' or 'full'.");
                }
            }
            else if (IsMatch(option, @"^[\/|-]time$"))
            {
                this.Configuration.EnableProfiling = true;
            }
            else if (IsMatch(option, @"^[\/|-]xsa$"))
            {
                this.Configuration.DoStateTransitionAnalysis = true;
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
            if (string.IsNullOrEmpty(this.Configuration.SolutionFilePath))
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
            if (this.Configuration.AnalyzeDataRaces)
            {
                this.Configuration.AnalyzeDataFlow = true;
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
