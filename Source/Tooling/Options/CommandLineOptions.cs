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

namespace Microsoft.PSharp.Tooling
{
    public class CommandLineOptions
    {
        #region fields

        private string[] Options;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public CommandLineOptions(string[] args)
        {
            this.Options = args;
        }

        /// <summary>
        /// Parses the command line options and assigns values to
        /// the global options for the analyser.
        /// </summary>
        public void Parse()
        {
            for (int idx = 0; idx < this.Options.Length; idx++)
            {
                #region core options

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
                else if (this.Options[idx].ToLower().StartsWith("/o:") &&
                    this.Options[idx].Length > 3)
                {
                    Configuration.OutputFilePath = this.Options[idx].Substring(3);
                }
                else if (this.Options[idx].ToLower().StartsWith("/output:") &&
                    this.Options[idx].Length > 8)
                {
                    Configuration.OutputFilePath = this.Options[idx].Substring(8);
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
                else if (this.Options[idx].ToLower().Equals("/noparsing"))
                {
                    Configuration.NoParsing = true;
                }
                else if (this.Options[idx].ToLower().Equals("/nocompile"))
                {
                    Configuration.NoCompilation = true;
                }
                else if (this.Options[idx].ToLower().StartsWith("/timeout:") &&
                    this.Options[idx].Length > 9)
                {
                    int i = 0;
                    if (!int.TryParse(this.Options[idx].Substring(9), out i))
                    {
                        ErrorReporter.ReportAndExit("Please give a valid timeout " +
                            "'/timeout:[x]', where [x] > 0.");
                    }

                    Configuration.AnalysisTimeout = i;
                }
                else if (this.Options[idx].ToLower().StartsWith("/v:") &&
                    this.Options[idx].Length > 3)
                {
                    int i = 1;
                    if (!int.TryParse(this.Options[idx].Substring(3), out i))
                    {
                        ErrorReporter.ReportAndExit("Please give a valid verbosity level " +
                            "'/v:[x]', where 0 <= [x] <= 2.");
                    }

                    Configuration.Verbose = i;
                }
                else if (this.Options[idx].ToLower().Equals("/debug"))
                {
                    Configuration.Debug = DebugType.All;
                }
                else if (this.Options[idx].ToLower().StartsWith("/debug:") &&
                    this.Options[idx].Length > 7)
                {
                    if (this.Options[idx].Substring(7).ToLower().Equals("all"))
                    {
                        Configuration.Debug = DebugType.All;
                    }
                    else if (this.Options[idx].Substring(7).ToLower().Equals("runtime"))
                    {
                        Configuration.Debug = DebugType.Runtime;
                    }
                    else if (this.Options[idx].Substring(7).ToLower().Equals("analysis"))
                    {
                        Configuration.Debug = DebugType.Analysis;
                    }
                    else if (this.Options[idx].Substring(7).ToLower().Equals("testing"))
                    {
                        Configuration.Debug = DebugType.Testing;
                    }
                    else
                    {
                        ErrorReporter.ReportAndExit("Please give a valid debug target '/debug:[x]', " +
                            "where [x] is 'all', 'runtime', 'analysis' or 'testing'.");
                    }
                }

                #endregion

                #region static analysis options

                else if (this.Options[idx].ToLower().Equals("/analyze"))
                {
                    Configuration.RunStaticAnalysis = true;
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
                else if (this.Options[idx].ToLower().Equals("/analyzeexceptions"))
                {
                    Configuration.AnalyzeExceptionHandling = true;
                }

                #endregion

                #region dynamic analysis options

                else if (this.Options[idx].ToLower().Equals("/test"))
                {
                    Configuration.RunDynamicAnalysis = true;
                }
                else if (this.Options[idx].ToLower().StartsWith("/sch:") &&
                    this.Options[idx].Length > 5)
                {
                    Configuration.SchedulingStrategy = this.Options[idx].Substring(5);
                }
                else if (this.Options[idx].ToLower().StartsWith("/i:") &&
                    this.Options[idx].Length > 3)
                {
                    int i = 1;
                    if (!int.TryParse(this.Options[idx].Substring(3), out i))
                    {
                        ErrorReporter.ReportAndExit("Please give a valid number of iterations " +
                            "'/i:[x]', where [x] > 0.");
                    }

                    Configuration.SchedulingIterations = i;
                }
                else if (this.Options[idx].ToLower().Equals("/explore"))
                {
                    Configuration.FullExploration = true;
                }

                #endregion

                #region error

                else
                {
                    ErrorReporter.ReportAndExit("cannot recognise command line option '" +
                        this.Options[idx] + "'.");
                }

                #endregion
            }

            this.CheckForParsingErrors();
        }

        #endregion

        #region private methods

        private void CheckForParsingErrors()
        {
            if (Configuration.SolutionFilePath.Equals(""))
            {
                ErrorReporter.ReportAndExit("Please give a valid solution path.");
            }

            if (!Configuration.SchedulingStrategy.Equals("") &&
                !Configuration.SchedulingStrategy.Equals("random") &&
                !Configuration.SchedulingStrategy.Equals("dfs"))
            {
                ErrorReporter.ReportAndExit("Please give a valid scheduling strategy " +
                    "'/sch:[x]', where [x] is 'random' or 'dfs'.");
            }
        }

        #endregion
    }
}
