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

using Microsoft.PSharp.BugFinding;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis
{
    /// <summary>
    /// The P# systematic concurrency testing engine.
    /// </summary>
    public static class SCTEngine
    {
        #region fields

        /// <summary>
        /// The bug-finding scheduling strategy.
        /// </summary>
        private static ISchedulingStrategy Strategy;

        /// <summary>
        /// Number of found bugs.
        /// </summary>
        private static int FoundBugs;

        /// <summary>
        /// Number of scheduling points.
        /// </summary>
        private static int SchedulingPoints;

        /// <summary>
        /// Explored schedules so far.
        /// </summary>
        private static int ExploredSchedules;

        /// <summary>
        /// A guard for printing info.
        /// </summary>
        private static int PrintGuard;
        
        #endregion

        #region public API

        /// <summary>
        /// Runs the P# compilation engine.
        /// </summary>
        public static void Run()
        {
            SCTEngine.Setup();
            SCTEngine.FindBugs();
            SCTEngine.Report();
        }

        #endregion

        #region private API

        /// <summary>
        /// Setups the runtime for systematic concurrency testing.
        /// </summary>
        private static void Setup()
        {
            SCTEngine.FoundBugs = 0;
            SCTEngine.SchedulingPoints = 0;
            SCTEngine.ExploredSchedules = 0;
            SCTEngine.PrintGuard = 1;

            if (AnalysisContext.Strategy == SchedulingStrategy.Random)
                SCTEngine.Strategy = new RandomSchedulingStrategy(DateTime.Now.Millisecond);
            else if (AnalysisContext.Strategy == SchedulingStrategy.DFS)
                SCTEngine.Strategy = new DFSSchedulingStrategy();
        }

        /// <summary>
        /// Explores the P# program for bugs.
        /// </summary>
        private static void FindBugs()
        {
            Console.WriteLine("... SCT using '{0}'", AnalysisContext.Strategy);

            Task task = new Task(() =>
            {
                for (int i = 0; i < Configuration.SchedulingIterations; i++)
                {
                    if (SCTEngine.ShouldPrintIteration(i + 1))
                    {
                        Console.WriteLine("..... Iteration #{0}", i + 1);
                    }

                    Runtime.BugFinder = new Scheduler(SCTEngine.Strategy);
                    var sw = SCTEngine.RedirectOutput();

                    AnalysisContext.EntryPoint.Invoke(null, null);

                    SCTEngine.ResetOutput();
                    
                    SCTEngine.ExploredSchedules++;
                    SCTEngine.SchedulingPoints = Runtime.BugFinder.SchedulingPoints;
                    if (Runtime.BugFinder.BugFound)
                    {
                        SCTEngine.FoundBugs++;
                    }

                    if (SCTEngine.Strategy.HasFinished())
                    {
                        break;
                    }

                    SCTEngine.Strategy.Reset();
                    if (!Configuration.FullExploration && SCTEngine.FoundBugs > 0)
                    {
                        if (sw != null)
                        {
                            var path = Path.GetDirectoryName(AnalysisContext.Assembly.Location) +
                            Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(
                                AnalysisContext.Assembly.Location) + ".txt";
                            Console.WriteLine("... Writing {0}", path);
                            File.WriteAllText(path, sw.ToString());
                        }
                        
                        break;
                    }
                }
            });

            Profiler.StartMeasuringExecutionTime();
            task.Start();

            try
            {
                if (Configuration.AnalysisTimeout > 0)
                {
                    task.Wait(Configuration.AnalysisTimeout * 1000);
                    if (!task.IsCompleted)
                    {
                        Runtime.Dispose();
                    }
                }
                else
                {
                    task.Wait();
                }
            }
            catch (AggregateException)
            {
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
        private static void Report()
        {
            Console.WriteLine("... Found {0} bug{1}.", SCTEngine.FoundBugs,
                SCTEngine.FoundBugs == 1 ? "" : "s");
            Console.WriteLine("... Explored {0} {1} schedule{2}.", SCTEngine.ExploredSchedules,
                SCTEngine.Strategy.HasFinished() ? "(all)" : "",
                SCTEngine.ExploredSchedules == 1 ? "" : "s");

            if (SCTEngine.ExploredSchedules > 0)
            {
                Console.WriteLine("... Found {0} % buggy schedules.",
                    (SCTEngine.FoundBugs * 100 / SCTEngine.ExploredSchedules));
                Console.WriteLine("... Instrumented {0} scheduling point{1} (on last iteration).",
                    SCTEngine.SchedulingPoints, SCTEngine.SchedulingPoints == 1 ? "" : "s");
            }

            Console.WriteLine("... Elapsed {0} sec.", Profiler.Results());
        }

        #endregion

        #region helper API

        /// <summary>
        /// Returns true if the engine should print the current iteration.
        /// </summary>
        /// <param name="iteration">Iteration</param>
        /// <returns>Boolean</returns>
        private static bool ShouldPrintIteration(int iteration)
        {
            if (iteration > SCTEngine.PrintGuard * 10)
            {
                var count = (iteration.ToString().Length - 1);
                var guard = "1" + (count > 0 ? String.Concat(Enumerable.Repeat("0", count)) : "");
                SCTEngine.PrintGuard = int.Parse(guard);
            }

            return iteration % SCTEngine.PrintGuard == 0;
        }

        /// <summary>
        /// Redirects the console output.
        /// </summary>
        /// <returns>StringWriter</returns>
        private static StringWriter RedirectOutput()
        {
            if (Configuration.Verbose == 2)
            {
                return null;
            }

            var sw = new StringWriter();
            Console.SetOut(sw);

            return sw;
        }

        /// <summary>
        /// Resets the console output.
        /// </summary>
        private static void ResetOutput()
        {
            if (Configuration.Verbose == 2)
            {
                return;
            }

            var sw = new StreamWriter(Console.OpenStandardOutput());
            sw.AutoFlush = true;
            Console.SetOut(sw);
        }

        #endregion
    }
}
