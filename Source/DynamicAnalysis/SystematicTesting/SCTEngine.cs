//-----------------------------------------------------------------------
// <copyright file="SCTEngine.cs">
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
using System.Linq;
using System.Threading.Tasks;

using Microsoft.PSharp.DynamicAnalysis.Scheduling;
using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis
{
    /// <summary>
    /// The P# systematic concurrency testing engine.
    /// </summary>
    public sealed class SCTEngine
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;


        /// <summary>
        /// The bug-finding scheduling strategy.
        /// </summary>
        private ISchedulingStrategy Strategy;

        /// <summary>
        /// Explored schedules so far.
        /// </summary>
        private int ExploredSchedules;

        /// <summary>
        /// A guard for printing info.
        /// </summary>
        private int PrintGuard;

        /// <summary>
        /// Has redirected console output.
        /// </summary>
        private bool HasRedirectedConsoleOutput;

        /// <summary>
        /// Number of found bugs.
        /// </summary>
        public int NumOfFoundBugs { get; private set; }

        /// <summary>
        /// Explored depth of scheduling decisions.
        /// </summary>
        public int ExploredDepth { get; private set; }

        /// <summary>
        /// The latest bug report, if any.
        /// </summary>
        public string BugReport { get; private set; }

        #endregion

        #region public API

        /// <summary>
        /// Creates a new systematic concurrency testing engine.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>SCTEngine</returns>
        public static SCTEngine Create(AnalysisContext context)
        {
            return new SCTEngine(context);
        }

        /// <summary>
        /// Runs the P# systematic testing engine.
        /// </summary>
        /// <returns>SCTEngine</returns>
        public SCTEngine Run()
        {
            this.FindBugs();
            this.Report();
            return this;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private SCTEngine(AnalysisContext context)
        {
            this.AnalysisContext = context;

            this.NumOfFoundBugs = 0;
            this.BugReport = "";
            this.ExploredDepth = 0;
            this.ExploredSchedules = 0;
            this.PrintGuard = 1;
            
            if (this.AnalysisContext.Configuration.SchedulingStrategy == SchedulingStrategy.Random)
            {
                this.Strategy = new RandomStrategy(this.AnalysisContext);
            }
            else if (this.AnalysisContext.Configuration.SchedulingStrategy == SchedulingStrategy.DFS)
            {
                this.Strategy = new DFSStrategy(this.AnalysisContext);
                this.AnalysisContext.Configuration.FullExploration = false;
            }
            else if (this.AnalysisContext.Configuration.SchedulingStrategy == SchedulingStrategy.IDDFS)
            {
                this.Strategy = new IterativeDeepeningDFSStrategy(this.AnalysisContext);
                this.AnalysisContext.Configuration.FullExploration = false;
            }
            else if (this.AnalysisContext.Configuration.SchedulingStrategy == SchedulingStrategy.MaceMC)
            {
                this.Strategy = new MaceMCStrategy(this.AnalysisContext);
                this.AnalysisContext.Configuration.FullExploration = false;
                this.AnalysisContext.Configuration.CheckLiveness = true;
                this.AnalysisContext.Configuration.CacheProgramState = false;
            }

            this.HasRedirectedConsoleOutput = false;
        }

        /// <summary>
        /// Explores the P# program for bugs.
        /// </summary>
        private void FindBugs()
        {
            Output.PrintLine("... Using '{0}' strategy", AnalysisContext.Configuration.SchedulingStrategy);

            Task task = new Task(() =>
            {
                for (int i = 0; i < this.AnalysisContext.Configuration.SchedulingIterations; i++)
                {
                    if (this.ShouldPrintIteration(i + 1))
                    {
                        Output.PrintLine("..... Iteration #{0}", i + 1);
                    }

                    if (this.AnalysisContext.Configuration.ScheduleIntraMachineConcurrency)
                    {
                        PSharpRuntime.BugFinder = new TaskAwareBugFindingScheduler(this.Strategy);
                    }
                    else
                    {
                        PSharpRuntime.BugFinder = new BugFindingScheduler(this.Strategy);
                    }

                    StringWriter sw = null;
                    if (this.AnalysisContext.Configuration.RedirectConsoleOutput &&
                        this.AnalysisContext.Configuration.Verbose < 2)
                    {
                        sw = this.RedirectConsoleOutput();
                        this.HasRedirectedConsoleOutput = true;
                    }

                    // Configure the test.
                    PSharpRuntime.Configure(this.AnalysisContext.Configuration);

                    // Start the test and wait for it to terminate.
                    this.AnalysisContext.TestMethod.Invoke(null, null);
                    PSharpRuntime.WaitMachines();

                    // Runs the liveness checker to find any liveness property violations.
                    // Requires that no bug has been found, the scheduler terminated before
                    // reaching the depth bound, and there is state caching is not active.
                    if (this.AnalysisContext.Configuration.CheckLiveness &&
                        !this.AnalysisContext.Configuration.CacheProgramState &&
                        !PSharpRuntime.BugFinder.BugFound)
                    {
                        PSharpRuntime.LivenessChecker.Run();
                    }

                    if (this.HasRedirectedConsoleOutput)
                    {
                        this.ResetOutput();
                    }

                    this.ExploredSchedules++;
                    this.ExploredDepth = PSharpRuntime.BugFinder.SchedulingPoints;

                    if (PSharpRuntime.BugFinder.BugFound)
                    {
                        this.NumOfFoundBugs++;
                        this.BugReport = PSharpRuntime.BugFinder.BugReport;
                    }
                    else
                    {
                        this.BugReport = "";
                    }

                    if (this.Strategy.HasFinished())
                    {
                        break;
                    }

                    this.Strategy.ConfigureNextIteration();

                    if (!this.AnalysisContext.Configuration.FullExploration &&
                      (this.NumOfFoundBugs > 0 || this.AnalysisContext.Configuration.PrintTrace))
                    {
                        if (sw != null && !this.AnalysisContext.Configuration.SuppressTrace)
                        {
                            this.PrintTrace(sw);
                        }
                        
                        break;
                    }
                }
            });

            Profiler.StartMeasuringExecutionTime();
            task.Start();

            try
            {
                if (this.AnalysisContext.Configuration.Timeout > 0)
                {
                    task.Wait(this.AnalysisContext.Configuration.Timeout * 1000);
                }
                else
                {
                    task.Wait();
                }
            }
            catch (AggregateException ex)
            {
                if (this.HasRedirectedConsoleOutput)
                {
                    this.ResetOutput();
                }

                Output.Debug(ex.Message);
                Output.Debug(ex.StackTrace);
                ErrorReporter.ReportAndExit("Internal systematic testing exception. " +
                    "Please send a bug report to the developers.");
            }
            finally
            {
                Profiler.StopMeasuringExecutionTime();
            }
        }

        /// <summary>
        /// Reports the testing results.
        /// </summary>
        private void Report()
        {
            Output.PrintLine("... Found {0} bug{1}.", this.NumOfFoundBugs,
                this.NumOfFoundBugs == 1 ? "" : "s");
            Output.PrintLine("... Explored {0} {1} schedule{2}.", this.ExploredSchedules,
                this.Strategy.HasFinished() ? "(all)" : "",
                this.ExploredSchedules == 1 ? "" : "s");

            if (this.ExploredSchedules > 0)
            {
                Output.PrintLine("... Found {0} % buggy schedules.",
                    (this.NumOfFoundBugs * 100 / this.ExploredSchedules));
                Output.PrintLine("... Instrumented {0} scheduling point{1} (on last iteration).",
                    this.ExploredDepth, this.ExploredDepth == 1 ? "" : "s");
            }

            if (this.AnalysisContext.Configuration.DepthBound > 0)
            {
                Output.PrintLine("... Used depth bound of {0}.", this.AnalysisContext.Configuration.DepthBound);
            }

            Output.PrintLine("... Elapsed {0} sec.", Profiler.Results());
        }

        /// <summary>
        /// Writes the trace in an output file.
        /// </summary>
        /// <param name="sw">StringWriter</param>
        private void PrintTrace(StringWriter sw)
        {
            var name = Path.GetFileNameWithoutExtension(this.AnalysisContext.Assembly.Location);
            var directory = Path.GetDirectoryName(this.AnalysisContext.Assembly.Location) +
                Path.DirectorySeparatorChar + "traces" + Path.DirectorySeparatorChar;

            Directory.CreateDirectory(directory);

            var traces = Directory.GetFiles(directory, name + "*.txt");
            var path = directory + name + "_" + traces.Length + ".txt";

            Output.PrintLine("... Writing {0}", path);
            File.WriteAllText(path, sw.ToString());
        }

        #endregion

        #region helper API

        /// <summary>
        /// Returns true if the engine should print the current iteration.
        /// </summary>
        /// <param name="iteration">Iteration</param>
        /// <returns>Boolean</returns>
        private bool ShouldPrintIteration(int iteration)
        {
            if (iteration > this.PrintGuard * 10)
            {
                var count = (iteration.ToString().Length - 1);
                var guard = "1" + (count > 0 ? String.Concat(Enumerable.Repeat("0", count)) : "");
                this.PrintGuard = int.Parse(guard);
            }

            return iteration % this.PrintGuard == 0;
        }

        /// <summary>
        /// Redirects the console output.
        /// </summary>
        /// <returns>StringWriter</returns>
        private StringWriter RedirectConsoleOutput()
        {
            var sw = new StringWriter();
            Console.SetOut(sw);
            return sw;
        }

        /// <summary>
        /// Resets the console output.
        /// </summary>
        private void ResetOutput()
        {
            var sw = new StreamWriter(Console.OpenStandardOutput());
            sw.AutoFlush = true;
            Console.SetOut(sw);
            this.HasRedirectedConsoleOutput = false;
        }

        #endregion
    }
}
