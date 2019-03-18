using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Utilities
{
    public sealed class MinimizerCommandLineOptions : BaseCommandLineOptions
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public MinimizerCommandLineOptions(string[] args)
            : base(args)
        {

        }

        /// <summary>
        /// Parses the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected override void ParseOption(string option)
        {
            if (this.IsMatch(option, @"^[\/|-]test:") && option.Length > 6)
            {
                base.Configuration.AssemblyToBeAnalyzed = option.Substring(6);
            }
            else if (this.IsMatch(option, @"^[\/|-]runtime:") && option.Length > 9)
            {
                base.Configuration.TestingRuntimeAssembly = option.Substring(9);
            }
            else if (this.IsMatch(option, @"^[\/|-]method:") && option.Length > 8)
            {
                base.Configuration.TestMethodName = option.Substring(8);
            }
            else if (this.IsMatch(option, @"^[\/|-]replay:") && option.Length > 8)
            {
                string extension = System.IO.Path.GetExtension(option.Substring(8));
                if (!extension.Equals(".schedule") && !extension.Equals(".mintrace"))
                {
                    Error.ReportAndExit("Please give a valid schedule file " +
                        "'-replay:[x]', where [x] has extension '.schedule'.");
                }

                base.Configuration.ScheduleFile = option.Substring(8);
            }
            else if (this.IsMatch(option, @"^[\/|-]break$"))
            {
                base.Configuration.AttachDebugger = true;
            }
            else if (this.IsMatch(option, @"^[\/|-]attach-debugger$"))
            {
                base.Configuration.AttachDebugger = true;
            }
            else if (this.IsMatch(option, @"^[\/|-]cycle-detection$"))
            {
                base.Configuration.EnableCycleDetection = true;
            }
            else if (this.IsMatch(option, @"^[\/|-]custom-state-hashing$"))
            {
                base.Configuration.EnableUserDefinedStateHashing = true;
            }
            else if (this.IsMatch(option, @"^[\/|-]i:") && option.Length > 3)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(3), out i) && i > 0)
                {
                    Error.ReportAndExit("Please give a valid number of " +
                        "iterations '-i:[x]', where [x] > 0.");
                }

                base.Configuration.SchedulingIterations = i;
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
                    "program's dll using '-test:[x]'.");
            }

            if (base.Configuration.ScheduleFile.Equals(""))
            {
                Error.ReportAndExit("Please give a valid path to a P# schedule " +
                    "file using '-replay:[x]', where [x] has extension '.schedule'.");
            }
        }

        /// <summary>
        /// Updates the configuration depending on the
        /// user specified options.
        /// </summary>
        protected override void UpdateConfiguration() { }

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
            help += "\n  -test:[x]\t Path to the P# program to test";
            help += "\n  -timeout:[x]\t Timeout (default is no timeout)";
            help += "\n  -v:[x]\t Enable verbose mode (values from '1' to '3')";

            help += "\n\n ------------------";
            help += "\n Replaying options:";
            help += "\n ------------------";
            help += "\n  -replay:[x]\t Schedule to replay";
            help += "\n  -break:[x]\t Attach debugger and break at bug";

            help += "\n";

            Output.WriteLine(help);
        }
    }
}
