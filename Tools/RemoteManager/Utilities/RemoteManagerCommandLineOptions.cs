//-----------------------------------------------------------------------
// <copyright file="RemoteManagerCommandLineOptions.cs">
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
using System.IO;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.Remote
{
    public sealed class RemoteManagerCommandLineOptions : BaseCommandLineOptions
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public RemoteManagerCommandLineOptions(string[] args)
            : base(args)
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
            if (option.ToLower().StartsWith("/n:") && option.Length > 3)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(3), out i) && i > 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid number of containers " +
                        "'/n:[x]', where [x] > 0.");
                }

                base.Configuration.NumberOfContainers = i;
            }
            else if (option.ToLower().StartsWith("/load:") && option.Length > 6)
            {
                base.Configuration.RemoteApplicationFilePath = option.Substring(6);
            }
            else
            {
                base.ParseOption(option);
            }
        }

        protected override void CheckForParsingErrors()
        {
            if (base.Configuration.RemoteApplicationFilePath.Equals(""))
            {
                ErrorReporter.ReportAndExit("Please give a valid P# application path.");
            }

            if (!Path.GetExtension(base.Configuration.RemoteApplicationFilePath).Equals(".dll"))
            {
                ErrorReporter.ReportAndExit("The application must be a `dll` file.");
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
            help += "\n  /load:[x]\t Path to the P# application to execute";
            help += "\n  /n:[x]\t Number of runtime containers to create (x > 0)";
            help += "\n  /timeout:[x]\t Timeout for the tool (default is no timeout)";
            help += "\n  /v:[x]\t Enable verbose mode (values from '1' to '3')";
            help += "\n  /debug\t Enable debugging";

            help += "\n";

            IO.PrettyPrintLine(help);
        }

        #endregion
    }
}
