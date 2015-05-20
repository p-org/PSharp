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
            Console.WriteLine("... SCT using '{0}'", AnalysisContext.Strategy);

            SCTEngine.Setup();

            Task task = new Task(() =>
            {
                for (int i = 0; i < Configuration.SchedulingIterations; i++)
                {
                    if (SCTEngine.ShouldPrintIteration(i + 1))
                    {
                        Console.WriteLine("..... Iteration #{0}", i + 1);
                    }
                    
                    AnalysisContext.EntryPoint.Invoke(null, null);
                    SCTEngine.ExploredSchedules++;
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

            Runtime.BugFinder = new BugFinder(scheduler);
        }

        /// <summary>
        /// Reports the testing results.
        /// </summary>
        private static void Report()
        {
            Runtime.BugFinder.Report();
            Console.WriteLine("... Explored {0} schedule{1}.", SCTEngine.ExploredSchedules,
                SCTEngine.ExploredSchedules == 1 ? "" : "s");
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

        #endregion
    }
}
