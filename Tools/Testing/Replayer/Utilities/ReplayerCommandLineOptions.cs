//-----------------------------------------------------------------------
// <copyright file="ReplayerCommandLineOptions.cs">
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

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.Utilities
{
    public sealed class ReplayerCommandLineOptions : BaseCommandLineOptions
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public ReplayerCommandLineOptions(string[] args)
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
            else if (option.ToLower().Equals("/attach-debugger") ||
                option.ToLower().Equals("/break"))
            {
                base.Configuration.AttachDebugger = true;
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

            if (base.Configuration.ScheduleFile.Equals(""))
            {
                Error.ReportAndExit("Please give a valid path to a P# schedule " +
                    "file using '/replay:[x]', where [x] has extension '.schedule'.");
            }
        }

        /// <summary>
        /// Updates the configuration depending on the
        /// user specified options.
        /// </summary>
        protected override void UpdateConfiguration()
        {

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
            help += "\n  /timeout:[x]\t Timeout (default is no timeout)";
            help += "\n  /v:[x]\t Enable verbose mode (values from '1' to '3')";

            help += "\n\n ------------------";
            help += "\n Replaying options:";
            help += "\n ------------------";
            help += "\n  /replay:[x]\t Schedule to replay";
            help += "\n  /break:[x]\t Attach debugger and break at bug";

            help += "\n";

            Output.WriteLine(help);
        }

        #endregion
    }
}
