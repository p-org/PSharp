//-----------------------------------------------------------------------
// <copyright file="AnalysisContext.cs">
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
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis
{
    /// <summary>
    /// The P# static analysis context.
    /// </summary>
    public static class AnalysisContext
    {
        #region fields

        /// <summary>
        /// The P# assembly to analyze.
        /// </summary>
        internal static Assembly Assembly;

        /// <summary>
        /// The scheduling strategy to use.
        /// </summary>
        internal static SchedulingStrategy Strategy;

        /// <summary>
        /// The entry point to the P# program.
        /// </summary>
        internal static MethodInfo EntryPoint;

        #endregion

        #region public API

        /// <summary>
        /// Create a new P# dynamic analysis context from the given assembly.
        /// </summary>
        /// <param name="assemblyName">Assembly name</param>
        public static void Create(string assemblyName)
        {
            try
            {
                AnalysisContext.Assembly = Assembly.LoadFrom(assemblyName);
            }
            catch (FileNotFoundException ex)
            {
                ErrorReporter.ReportErrorAndExit(ex.Message);
            }

            // Setups the scheduling strategy.
            AnalysisContext.SetupSchedulingStrategy();
            
            // Finds the entry point to the P# program.
            AnalysisContext.FindEntryPoint();
        }

        /// <summary>
        /// Prints program statistics.
        /// </summary>
        public static void PrintStatistics()
        {
            //Console.WriteLine("Number of machines in the program: {0}",
            //    StateTransitionAnalysis.NumOfMachines);
            //Console.WriteLine("Number of state transitions in the program: {0}",
            //    StateTransitionAnalysis.NumOfTransitions);
            //Console.WriteLine("Number of action bindings in the program: {0}",
            //    StateTransitionAnalysis.NumOfActionBindings);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Finds the entry point to the P# program.
        /// </summary>
        private static void FindEntryPoint()
        {
            var entrypoints = AnalysisContext.Assembly.GetTypes().SelectMany(t => t.GetMethods()).
                Where(m => m.GetCustomAttributes(typeof(EntryPoint), false).Length > 0).ToList();
            if (entrypoints.Count == 0)
            {
                ErrorReporter.ReportErrorAndExit("No entry point found to the P# program. " +
                    "Use the attribute [EntryPoint] to declare an entry point method.");
            }
            else if (entrypoints.Count > 1)
            {
                ErrorReporter.ReportErrorAndExit("Only one entry point to the P# program can be declared. " +
                    "{0} entry points were found instead.", entrypoints.Count);
            }

            AnalysisContext.EntryPoint = entrypoints[0];
        }

        /// <summary>
        /// Setups the scheduling strategy.
        /// </summary>
        private static void SetupSchedulingStrategy()
        {
            if (Configuration.SchedulingStrategy.Equals("") ||
                Configuration.SchedulingStrategy.Equals("random"))
            {
                AnalysisContext.Strategy = SchedulingStrategy.Random;
            }
            else if (Configuration.SchedulingStrategy.Equals("dfs"))
            {
                AnalysisContext.Strategy = SchedulingStrategy.DFS;
            }
        }

        #endregion
    }
}
