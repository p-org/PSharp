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
        /// Number of found bugs.
        /// </summary>
        private static int FoundBugs;

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
            SCTEngine.ExploredSchedules = 0;
            SCTEngine.PrintGuard = 1;

            IScheduler scheduler = null;
            if (AnalysisContext.Strategy == SchedulingStrategy.Random)
                scheduler = new RandomScheduler(DateTime.Now.Millisecond);
            else if (AnalysisContext.Strategy == SchedulingStrategy.DFS)
                scheduler = new DFSScheduler();

            Runtime.Options.FindBugs = true;

            Runtime.Options.PrintScheduleInfo = true;
            Runtime.Options.Verbose = true;

            Runtime.BugFinder = new BugFinder(scheduler);
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

                    Runtime.BugFinder.Start();
                    var sw = SCTEngine.RedirectOutput();

                    AnalysisContext.EntryPoint.Invoke(null, null);

                    SCTEngine.ResetOutput();
                    SCTEngine.ExploredSchedules++;
                    if (Runtime.BugFinder.BugFound)
                    {
                        SCTEngine.FoundBugs++;
                    }

                    Runtime.BugFinder.Reset();
                    if (!Configuration.FullExploration && SCTEngine.FoundBugs > 0)
                    {
                        var path = Path.GetDirectoryName(AnalysisContext.Assembly.Location) +
                            Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(
                                AnalysisContext.Assembly.Location) + ".txt";
                        Console.WriteLine("... Writing {0}", path);
                        File.WriteAllText(path, sw.ToString());
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
                }
                else
                {
                    task.Wait();
                }
            }
            catch (AggregateException ex)
            {
                ErrorReporter.ReportErrorAndExit(ex.Message);
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
            Console.WriteLine("... Explored {0} schedule{1}.", SCTEngine.ExploredSchedules,
                SCTEngine.ExploredSchedules == 1 ? "" : "s");
            Console.WriteLine("... Found {0} % buggy schedules.",
                (SCTEngine.FoundBugs * 100 / SCTEngine.ExploredSchedules));
            Console.WriteLine("... Elapsed {0} sec.", Profiler.Results());
        }

        #endregion

        #region helper API

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

        #endregion
    }
}
