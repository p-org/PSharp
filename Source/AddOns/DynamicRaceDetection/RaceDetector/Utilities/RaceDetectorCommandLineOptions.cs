// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Utilities
{
    public sealed class RaceDetectorCommandLineOptions : BaseCommandLineOptions
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public RaceDetectorCommandLineOptions(string[] args)
            : base(args)
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
            else if (option.ToLower().Equals("/time"))
            {
                base.Configuration.EnableProfiling = true;
            }
            else if (option.ToLower().Equals("/?"))
            {
                this.ShowHelp();
                System.Environment.Exit(0);
            }
            else
            {
                return;
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
        /// Checks for parsing errors.
        /// </summary>
        protected override void CheckForParsingErrors()
        {
            if (base.Configuration.AssemblyToBeAnalyzed.Equals(""))
            {
                IO.Error.ReportAndExit("Please give a valid path to a P# " +
                    "program's dll using '/test:[x]'.");
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
            help += "\n  /method:[x]\t Suffix of the test method to execute";
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

            help += "\n\n ---------------------";
            help += "\n Experimental options:";
            help += "\n ---------------------";
            help += "\n  /tpl\t Enable intra-machine concurrency scheduling";

            help += "\n";

            Output.WriteLine(help);
        }

        #endregion
    }
}