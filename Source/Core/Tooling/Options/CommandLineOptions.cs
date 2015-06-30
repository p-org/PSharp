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

        protected string[] Options;

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
        /// Parses the command line options.
        /// </summary>
        public virtual void Parse()
        {
            for (int idx = 0; idx < this.Options.Length; idx++)
            {
                #region core options

                if (this.Options[idx].ToLower().Equals("/?"))
                {
                    this.ShowHelp();
                    Environment.Exit(1);
                }
                else if (this.Options[idx].ToLower().StartsWith("/s:") &&
                    this.Options[idx].Length > 3)
                {
                    Configuration.SolutionFilePath = this.Options[idx].Substring(3);
                }
                else if (this.Options[idx].ToLower().StartsWith("/p:") &&
                    this.Options[idx].Length > 3)
                {
                    Configuration.ProjectName = this.Options[idx].Substring(3);
                }
                else if (this.Options[idx].ToLower().StartsWith("/o:") &&
                    this.Options[idx].Length > 3)
                {
                    Configuration.OutputFilePath = this.Options[idx].Substring(3);
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
                    if (!int.TryParse(this.Options[idx].Substring(9), out i) &&
                        i > 0)
                    {
                        ErrorReporter.ReportAndExit("Please give a valid timeout " +
                            "'/timeout:[x]', where [x] > 0.");
                    }

                    Configuration.AnalysisTimeout = i;
                }
                else if (this.Options[idx].ToLower().StartsWith("/v:") &&
                    this.Options[idx].Length > 3)
                {
                    int i = 0;
                    if (!int.TryParse(this.Options[idx].Substring(3), out i) &&
                        i >= 0 && i <= 2)
                    {
                        ErrorReporter.ReportAndExit("Please give a valid verbosity level " +
                            "'/v:[x]', where 0 <= [x] <= 2.");
                    }

                    Configuration.Verbose = i;
                }
                else if (this.Options[idx].ToLower().Equals("/debug"))
                {
                    Configuration.Debug.Add(DebugType.All);
                }
                else if (this.Options[idx].ToLower().StartsWith("/debug:") &&
                    this.Options[idx].Length > 7)
                {
                    if (this.Options[idx].Substring(7).ToLower().Equals("all"))
                    {
                        Configuration.Debug.Add(DebugType.All);
                    }
                    else if (this.Options[idx].Substring(7).ToLower().Equals("runtime"))
                    {
                        Configuration.Debug.Add(DebugType.Runtime);
                    }
                    else if (this.Options[idx].Substring(7).ToLower().Equals("analysis"))
                    {
                        Configuration.Debug.Add(DebugType.Analysis);
                    }
                    else if (this.Options[idx].Substring(7).ToLower().Equals("testing"))
                    {
                        Configuration.Debug.Add(DebugType.Testing);
                    }
                    else
                    {
                        ErrorReporter.ReportAndExit("Please give a valid debug target '/debug:[x]', " +
                            "where [x] is 'all', 'runtime', 'analysis' or 'testing'.");
                    }
                }

                #endregion

                #region compilation options

                else if (this.Options[idx].ToLower().Equals("/distributed"))
                {
                    Configuration.CompileForDistribution = true;
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
                else if (this.Options[idx].ToLower().StartsWith("/test:") &&
                    this.Options[idx].Length > 6)
                {
                    Configuration.RunDynamicAnalysis = true;
                    Configuration.ProjectName = this.Options[idx].Substring(6);
                }
                else if (this.Options[idx].ToLower().StartsWith("/sch:") &&
                    this.Options[idx].Length > 5)
                {
                    Configuration.SchedulingStrategy = this.Options[idx].Substring(5);
                }
                else if (this.Options[idx].ToLower().StartsWith("/i:") &&
                    this.Options[idx].Length > 3)
                {
                    int i = 0;
                    if (!int.TryParse(this.Options[idx].Substring(3), out i) &&
                        i > 0)
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
                else if (this.Options[idx].ToLower().StartsWith("/db:") &&
                    this.Options[idx].Length > 4)
                {
                    int i = 0;
                    if (!int.TryParse(this.Options[idx].Substring(4), out i) &&
                        i >= 0)
                    {
                        ErrorReporter.ReportAndExit("Please give a valid exploration depth " +
                            "bound '/i:[x]', where [x] >= 0.");
                    }

                    Configuration.DepthBound = i;
                }

                #endregion

                #region error

                else
                {
                    this.ShowHelp();
                    ErrorReporter.ReportAndExit("cannot recognise command line option '" +
                        this.Options[idx] + "'.");
                }

                #endregion
            }

            this.CheckForParsingErrors();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Checks for parsing errors.
        /// </summary>
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

        /// <summary>
        /// Shows help.
        /// </summary>
        private void ShowHelp()
        {
            string help = "\n";

            help += "--------------";
            help += "\nBasic options:";
            help += "\n--------------";
            help += "\n  /?\t\t Show this help menu";
            help += "\n  /s:\t\t Path to a P# solution";
            help += "\n  /p:\t\t Path to a project of a P# solution";
            help += "\n  /o:\t\t Path for output files";
            help += "\n  /timeout:\t Timeout for the tool";
            help += "\n  /v:\t\t Enable verbose mode (values from 0 to 2)";
            help += "\n  /debug\t Enable debugging";

            help += "\n\n--------------------";
            help += "\nCompilation options:";
            help += "\n--------------------";
            help += "\n  /ditributed\t Compile the P# program using the distributed runtime";

            help += "\n\n---------------------------";
            help += "\nSystematic testing options:";
            help += "\n---------------------------";
            help += "\n  /test\t\t Enable the systematic testing mode to find bugs";
            help += "\n  /i:\t\t Number of schedules to explore for bugs";
            help += "\n  /db:\t\t The depth bound (by default is 1000)";

            help += "\n";

            Output.WriteLine(help);
        }

        #endregion
    }
}
