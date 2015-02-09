//-----------------------------------------------------------------------
// <copyright file="CommandLineOptions.cs">
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
using System.Collections.Generic;
using System.Linq;

namespace PSharp
{
    internal class CommandLineOptions
    {
        #region fields

        private string[] Options;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        internal CommandLineOptions(string[] args)
        {
            this.Options = args;
        }

        /// <summary>
        /// Parses the command line options and assigns values to
        /// the global options for the analyser.
        /// </summary>
        internal void Parse()
        {
            for (int idx = 0; idx < this.Options.Length; idx++)
            {
                if (this.Options[idx].ToLower().StartsWith("/s:") &&
                    this.Options[idx].Length > 3)
                {
                    Configuration.SolutionFilePath = this.Options[idx].Substring(3);
                }
                else if (this.Options[idx].ToLower().StartsWith("/solution:") &&
                    this.Options[idx].Length > 10)
                {
                    Configuration.SolutionFilePath = this.Options[idx].Substring(10);
                }
                else if (this.Options[idx].ToLower().StartsWith("/p:") &&
                    this.Options[idx].Length > 3)
                {
                    Configuration.ProjectName = this.Options[idx].Substring(3);
                }
                else if (this.Options[idx].ToLower().StartsWith("/project:") &&
                    this.Options[idx].Length > 9)
                {
                    Configuration.ProjectName = this.Options[idx].Substring(9);
                }
                else if (this.Options[idx].ToLower().Equals("/showwarnings"))
                {
                    Configuration.ShowWarnings = true;
                }
                else if (this.Options[idx].ToLower().Equals("/showgivesup"))
                {
                    Configuration.ShowGivesUpInformation = true;
                }
                else if (this.Options[idx].ToLower().Equals("/showstatistics") ||
                    this.Options[idx].ToLower().Equals("/stats"))
                {
                    Configuration.ShowProgramStatistics = true;
                }
                else if (this.Options[idx].ToLower().Equals("/time"))
                {
                    Configuration.ShowRuntimeResults = true;
                }
                else if (this.Options[idx].ToLower().Equals("/timedfa"))
                {
                    Configuration.ShowDFARuntimeResults = true;
                }
                else if (this.Options[idx].ToLower().Equals("/timeroa"))
                {
                    Configuration.ShowROARuntimeResults = true;
                }
                else if (this.Options[idx].ToLower().Equals("/nostatetransitionanalysis"))
                {
                    Configuration.DoStateTransitionAnalysis = false;
                }
                else if (this.Options[idx].ToLower().Equals("/analyseexceptions"))
                {
                    Configuration.AnalyseExceptionHandling = true;
                }
            }

            this.CheckForParsingErrors();
        }

        #endregion

        #region private methods

        private void CheckForParsingErrors()
        {
            if (Configuration.SolutionFilePath.Equals(""))
            {
                ErrorReporter.Report("Please give a valid solution path.");
                Environment.Exit(1);
            }
        }

        #endregion
    }
}
