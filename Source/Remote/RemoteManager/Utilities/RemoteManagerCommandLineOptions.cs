//-----------------------------------------------------------------------
// <copyright file="RemoteManagerCommandLineOptions.cs">
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
using System.IO;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Remote
{
    public class RemoteManagerCommandLineOptions : CommandLineOptions
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

        /// <summary>
        /// Parses the command line options.
        /// </summary>
        public override void Parse()
        {
            for (int idx = 0; idx < base.Options.Length; idx++)
            {
                #region cluster manager options

                if (this.Options[idx].ToLower().StartsWith("/n:") &&
                    this.Options[idx].Length > 3)
                {
                    int i = 0;
                    if (!int.TryParse(this.Options[idx].Substring(3), out i) &&
                        i > 0)
                    {
                        ErrorReporter.ReportAndExit("Please give a valid number of containers " +
                            "'/n:[x]', where [x] > 0.");
                    }

                    Configuration.NumberOfContainers = i;
                }
                else if (base.Options[idx].ToLower().StartsWith("/main:") &&
                    base.Options[idx].Length > 6)
                {
                    Configuration.ApplicationFilePath = base.Options[idx].Substring(6);
                }

                #endregion

                #region error

                else
                {
                    ErrorReporter.ReportAndExit("cannot recognise command line option '" +
                        base.Options[idx] + "'.");
                }

                #endregion
            }

            this.CheckForParsingErrors();
        }

        #endregion

        #region private methods

        private void CheckForParsingErrors()
        {
            if (Configuration.ApplicationFilePath.Equals(""))
            {
                ErrorReporter.ReportAndExit("Please give a valid P# application path.");
            }

            if (!Path.GetExtension(Configuration.ApplicationFilePath).Equals(".dll"))
            {
                ErrorReporter.ReportAndExit("The application must be a `dll` file compiled " +
                    "using the P# compiler with the option `/distributed`.");
            }
        }

        #endregion
    }
}
