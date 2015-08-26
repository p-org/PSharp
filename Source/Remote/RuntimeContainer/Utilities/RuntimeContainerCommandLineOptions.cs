//-----------------------------------------------------------------------
// <copyright file="RuntimeContainerCommandLineOptions.cs">
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

using System;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Remote
{
    public sealed class RuntimeContainerCommandLineOptions : BaseCommandLineOptions
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public RuntimeContainerCommandLineOptions(string[] args)
            : base(args)
        {
            base.Configuration = new RuntimeContainerConfiguration();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parse the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected override void ParseOption(string option)
        {
            var configuration = base.Configuration as RuntimeContainerConfiguration;
            if (option.ToLower().StartsWith("/id:") && option.Length > 4)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(4), out i) && i >= 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid container id");
                }

                configuration.ContainerId = i;
            }
            else if (option.ToLower().StartsWith("/main:") &&
                option.Length > 6)
            {
                configuration.ApplicationFilePath = option.Substring(6);
            }
            else
            {
                base.ParseOption(option);
            }
        }

        protected override void CheckForParsingErrors()
        {
            var configuration = base.Configuration as RuntimeContainerConfiguration;
            if (configuration.ApplicationFilePath.Equals(""))
            {
                ErrorReporter.ReportAndExit("Please give a valid P# application path.");
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
            help += "\n  /timeout:[x]\t Timeout for the tool (default is no timeout)";
            help += "\n  /v:[x]\t Enable verbose mode (values from '0' to '3')";
            help += "\n  /debug\t Enable debugging";

            help += "\n";

            Output.PrettyPrintLine(help);
        }

        #endregion
    }
}
